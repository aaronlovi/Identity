import { Pool } from 'pg';

export const pool = new Pool({
    host: process.env.DB_HOST ?? "localhost",
    port: +(process.env.DB_PORT ?? "5432"),
    database: process.env.DB_DATABASE ?? "identity",
    user: process.env.DB_USER ?? "postgres",
    password: process.env.DB_PASSWORD ?? "postgres",
});

export const schema = process.env.DB_DATABASE ?? "identity";
export const q = (table: string) => `${schema}.${table}`;
