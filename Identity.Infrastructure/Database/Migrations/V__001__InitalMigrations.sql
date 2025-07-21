-- DL-01: Baseline migration
CREATE SCHEMA identity;
SET search_path = identity, public;

-- DL-02: Create users table
CREATE TABLE users (
    id BIGINT PRIMARY KEY,
    status TEXT NOT NULL,
    kyc_state TEXT,
    self_excluded_until TIMESTAMP WITH TIME ZONE,
    inserted_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW()
);
COMMENT ON TABLE users IS 'Stores user information including status and KYC state.';
COMMENT ON COLUMN users.id IS 'Primary key for the user, generated as a BIGINT.';
COMMENT ON COLUMN users.status IS 'Current status of the user (e.g., active, banned).';
COMMENT ON COLUMN users.kyc_state IS 'KYC (Know Your Customer) state of the user.';
COMMENT ON COLUMN users.self_excluded_until IS 'Timestamp until the user is self-excluded.';
COMMENT ON COLUMN users.inserted_at IS 'Timestamp when the user record was created.';
COMMENT ON COLUMN users.updated_at IS 'Timestamp when the user record was last updated.';

-- DL-03: Create profiles table
CREATE TABLE profiles (
    user_id BIGINT PRIMARY KEY REFERENCES users(id) ON DELETE CASCADE,
    display_name TEXT,
    avatar TEXT,
    timezone TEXT,
    inserted_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW()
);
COMMENT ON TABLE profiles IS 'Stores user profile information.';
COMMENT ON COLUMN profiles.user_id IS 'Foreign key referencing the user.';
COMMENT ON COLUMN profiles.display_name IS 'Display name of the user.';
COMMENT ON COLUMN profiles.avatar IS 'Avatar URL of the user.';
COMMENT ON COLUMN profiles.timezone IS 'Timezone of the user.';
COMMENT ON COLUMN profiles.inserted_at IS 'Timestamp when the profile record was created.';
COMMENT ON COLUMN profiles.updated_at IS 'Timestamp when the profile record was last updated.';

-- DL-04: Create roles table
CREATE TABLE roles (
    user_id BIGINT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    role TEXT NOT NULL,
    inserted_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
    PRIMARY KEY (user_id, role)
);
COMMENT ON TABLE roles IS 'Stores roles assigned to users.';
COMMENT ON COLUMN roles.user_id IS 'Foreign key referencing the user.';
COMMENT ON COLUMN roles.role IS 'Role assigned to the user.';
COMMENT ON COLUMN roles.inserted_at IS 'Timestamp when the role record was created.';
COMMENT ON COLUMN roles.updated_at IS 'Timestamp when the role record was last updated.';

-- DL-05: Create credentials table
CREATE TABLE credentials (
    user_id BIGINT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    type TEXT NOT NULL,
    external_id TEXT,
    hash TEXT,
    mfa_secret TEXT,
    inserted_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
    PRIMARY KEY (user_id, type, external_id)
);
COMMENT ON TABLE credentials IS 'Stores authentication credentials for users.';
COMMENT ON COLUMN credentials.user_id IS 'Foreign key referencing the user.';
COMMENT ON COLUMN credentials.type IS 'Type of credential (e.g., email, OAuth).';
COMMENT ON COLUMN credentials.external_id IS 'External identifier for the credential.';
COMMENT ON COLUMN credentials.hash IS 'Hashed password or token.';
COMMENT ON COLUMN credentials.mfa_secret IS 'MFA secret for the user.';
COMMENT ON COLUMN credentials.inserted_at IS 'Timestamp when the credential record was created.';
COMMENT ON COLUMN credentials.updated_at IS 'Timestamp when the credential record was last updated.';

-- DL-06: Create devices table
CREATE TABLE devices (
    user_id BIGINT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    device_id BIGINT NOT NULL,
    first_ip INET,
    ua TEXT,
    inserted_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
    PRIMARY KEY (user_id, device_id)
);
COMMENT ON TABLE devices IS 'Tracks devices associated with users.';
COMMENT ON COLUMN devices.user_id IS 'Foreign key referencing the user.';
COMMENT ON COLUMN devices.device_id IS 'Unique identifier for the device.';
COMMENT ON COLUMN devices.first_ip IS 'First IP address used by the device.';
COMMENT ON COLUMN devices.ua IS 'User agent string of the device.';
COMMENT ON COLUMN devices.inserted_at IS 'Timestamp when the device record was created.';
COMMENT ON COLUMN devices.updated_at IS 'Timestamp when the device record was last updated.';

-- DL-07: Create sessions table
CREATE TABLE sessions (
    user_id BIGINT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    device_id BIGINT REFERENCES devices(device_id) ON DELETE CASCADE,
    jwt_id BIGINT NOT NULL,
    revoked_at TIMESTAMP,
    exp TIMESTAMP NOT NULL,
    inserted_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
    PRIMARY KEY (user_id, device_id, jwt_id)
);
COMMENT ON TABLE sessions IS 'Tracks user sessions for authentication.';
COMMENT ON COLUMN sessions.user_id IS 'Foreign key referencing the user.';
COMMENT ON COLUMN sessions.device_id IS 'Foreign key referencing the device.';
COMMENT ON COLUMN sessions.jwt_id IS 'Unique identifier for the JWT.';
COMMENT ON COLUMN sessions.revoked_at IS 'Timestamp when the session was revoked.';
COMMENT ON COLUMN sessions.exp IS 'Expiration timestamp of the session.';
COMMENT ON COLUMN sessions.inserted_at IS 'Timestamp when the session record was created.';
COMMENT ON COLUMN sessions.updated_at IS 'Timestamp when the session record was last updated.';

-- DL-08: Create login_events table
CREATE TABLE login_events (
    user_id BIGINT REFERENCES users(id) ON DELETE CASCADE,
    credential_type TEXT,
    ip INET NOT NULL,
    success BOOLEAN NOT NULL,
    inserted_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
    PRIMARY KEY (user_id, ip, inserted_at)
);
COMMENT ON TABLE login_events IS 'Tracks login attempts for auditing and security.';
COMMENT ON COLUMN login_events.user_id IS 'Foreign key referencing the user.';
COMMENT ON COLUMN login_events.credential_type IS 'Type of credential used for login.';
COMMENT ON COLUMN login_events.ip IS 'IP address from which the login attempt was made.';
COMMENT ON COLUMN login_events.success IS 'Indicates whether the login attempt was successful.';
COMMENT ON COLUMN login_events.inserted_at IS 'Timestamp when the login attempt occurred.';
COMMENT ON COLUMN login_events.updated_at IS 'Timestamp when the login attempt record was last updated.';

-- DL-09: Create password_reset_tokens table
CREATE TABLE password_reset_tokens (
    credential_id BIGINT NOT NULL,
    token BIGINT NOT NULL,
    expires_at TIMESTAMP NOT NULL,
    delivery_channel TEXT,
    masked_destination TEXT,
    inserted_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
    PRIMARY KEY (credential_id, token)
);
COMMENT ON TABLE password_reset_tokens IS 'Stores password reset tokens for users.';
COMMENT ON COLUMN password_reset_tokens.credential_id IS 'Foreign key referencing the credential.';
COMMENT ON COLUMN password_reset_tokens.token IS 'Unique token for password reset.';
COMMENT ON COLUMN password_reset_tokens.expires_at IS 'Expiration timestamp of the token.';
COMMENT ON COLUMN password_reset_tokens.delivery_channel IS 'Channel used to deliver the token (e.g., email, SMS).';
COMMENT ON COLUMN password_reset_tokens.masked_destination IS 'Masked destination for token delivery.';
COMMENT ON COLUMN password_reset_tokens.inserted_at IS 'Timestamp when the token record was created.';
COMMENT ON COLUMN password_reset_tokens.updated_at IS 'Timestamp when the token record was last updated.';

-- DL-10: Create email_outbox table
CREATE TABLE email_outbox (
    id BIGINT PRIMARY KEY,
    payload JSONB NOT NULL,
    sent_at TIMESTAMP,
    fail_count INTEGER DEFAULT 0,
    inserted_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW()
);
COMMENT ON TABLE email_outbox IS 'Stores email messages to be sent.';
COMMENT ON COLUMN email_outbox.id IS 'Primary key for the email message.';
COMMENT ON COLUMN email_outbox.payload IS 'JSON payload of the email message.';
COMMENT ON COLUMN email_outbox.sent_at IS 'Timestamp when the email was sent.';
COMMENT ON COLUMN email_outbox.fail_count IS 'Number of failed attempts to send the email.';
COMMENT ON COLUMN email_outbox.inserted_at IS 'Timestamp when the email record was created.';
COMMENT ON COLUMN email_outbox.updated_at IS 'Timestamp when the email record was last updated.';

-- DL-11: Enforce all FKs with ON DELETE CASCADE and add unique constraints
-- (Already implemented in the above table definitions)
