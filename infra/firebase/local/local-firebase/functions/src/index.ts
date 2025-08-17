import { PubSub } from '@google-cloud/pubsub';
import { initializeApp } from "firebase-admin/app";
import { getAuth } from "firebase-admin/auth";
import * as logger from "firebase-functions/logger";
import { auth } from "firebase-functions/v1";
import { beforeUserSignedIn, AuthBlockingEvent } from "firebase-functions/v2/identity";
import { pool, q } from "./db";

// Boot admin SDK (emulator-aware)
initializeApp();

const pubsub = new PubSub({ projectId: process.env.GCLOUD_PROJECT || "demo-identity" });

/**
 * C-1: When a Firebase user is created (emulator), set default custom claims.
 * Later you'll also upsert the `users` row & publish UserCreated.
 */
export const userCreated = auth.user().onCreate(async (user) => {
    const roles = ["player"];
    const status = "active";

    // 1) durable claims on account
    await getAuth().setCustomUserClaims(user.uid, { roles, status });

    // 2) upsert DB row
    const userId = await provisionUser(user.uid);

    // 3) publish UserCreated for Orleans/Wallet/etc.
    await publishUserCreated({
        user_id: userId,
        firebase_uid: user.uid,
        roles,
        status,
    });

    logger.info("Provisioned user", { uid: user.uid, userId, roles, status });
});

/**
 * C-2: Before each sign-in, ensure the token carries current claims.
 * (For now we return defaults; later, read roles/status from your DB.)
 */
export const refreshClaims = beforeUserSignedIn(async (evt: AuthBlockingEvent) => {
    const uid = evt.data?.uid;

    if (!uid) {
        logger.warn("Missing uid in beforeUserSignedIn event", { evt });
        return { customClaims: { roles: ["player"], status: "active" } };
    }

    // Get user_id
    const userRow = await pool.query(
        `SELECT user_id FROM ${q("users")} WHERE firebase_uid = $1`,
        [uid]
    );
    if (!userRow.rowCount) {
        // Safety: if user row missing, return minimal defaults
        return { customClaims: { roles: ["player"], status: "active" } };
    }
    const userId = userRow.rows[0].user_id as number;

    // Load roles
    const rolesRes = await pool.query(
        `SELECT role FROM ${q("user_roles")} WHERE user_id = $1 ORDER BY role`,
        [userId]
    );
    const roles = rolesRes.rows.map(row => row.role) as string[];
    const rolesFinal = roles.length ? roles : ["player"];

    // Load status
    const statusRes = await pool.query(
        `SELECT status FROM ${q("user_status")} WHERE user_id = $1`,
        [userId]
    );
    const status = statusRes.rowCount ? (statusRes.rows[0].status as string) : "active";

    return { customClaims: { roles: rolesFinal, status } };
});

async function provisionUser(firebaseUid: string) {
    const client = await pool.connect();
    try {
        await client.query("BEGIN");

        // 1) users
        const ins = await client.query(
            `INSERT INTO ${q("users")} (firebase_uid, created_at, updated_at)
            VALUES ($1, ${q("now_utc")}(), ${q("now_utc")}())
            ON CONFLICT (firebase_uid) DO UPDATE
                SET updated_at = ${q("now_utc")}()
            RETURNING user_id`,
            [firebaseUid]
        );

        let userId: number;
        if (ins.rows.length) {
            userId = ins.rows[0].user_id;
        } else {
            const sel = await client.query(
                `SELECT user_id FROM ${q("users")} WHERE firebase_uid = $1`,
                [firebaseUid]
            );
            userId = sel.rows[0].user_id;
        }

        // 2) roles: ensure "player"
        await client.query(
            `INSERT INTO ${q("user_roles")} (user_id, role, assigned_at)
            VALUES ($1, 'player', ${q("now_utc")}())
            ON CONFLICT (user_id, role) DO NOTHING`,
            [userId]
        );

        // 3) status: ensure "active"
        await client.query(
            `INSERT INTO ${q("user_status")} (user_id, status)
            VALUES ($1, 'active')
            ON CONFLICT (user_id) DO UPDATE
                SET status = EXCLUDED.status, updated_at = ${q("now_utc")}()`,
            [userId]
        );

        await client.query("COMMIT");
        return userId;
    } catch (e) {
        await client.query("ROLLBACK");
        throw e;
    } finally {
        client.release();
    }
}

async function publishUserCreated(evt: {
    user_id: number; firebase_uid: string; roles: string[]; status: string;
}) {
    const topic = pubsub.topic("identity.events");
    const [exists] = await topic.exists();
    if (!exists) await pubsub.createTopic("identity.events");

    await topic.publishMessage({
        json: {
            specversion: "1.0",
            type: "UserCreated",
            source: "identity.functions",
            time: new Date().toISOString(),
            data: evt,
        },
    });
}
