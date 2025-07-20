-- DL-01: Create users table
CREATE TABLE users (
    id BIGINT PRIMARY KEY,
    email VARCHAR(255) NOT NULL UNIQUE,
    hashed_password TEXT NOT NULL,
    status VARCHAR(50) NOT NULL DEFAULT 'active',
    inserted_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW()
);
COMMENT ON TABLE users IS 'Stores user information including credentials and status.';
COMMENT ON COLUMN users.id IS 'Primary key for the user, generated as a BIGINT.';
COMMENT ON COLUMN users.email IS 'Unique email address for the user.';
COMMENT ON COLUMN users.hashed_password IS 'Hashed password for authentication.';
COMMENT ON COLUMN users.status IS 'Current status of the user (e.g., active, banned).';
COMMENT ON COLUMN users.inserted_at IS 'Timestamp when the user record was created.';
COMMENT ON COLUMN users.updated_at IS 'Timestamp when the user record was last updated.';

-- DL-02: Create roles table
CREATE TABLE roles (
    id BIGINT PRIMARY KEY,
    name VARCHAR(100) NOT NULL UNIQUE,
    description TEXT,
    inserted_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW()
);
COMMENT ON TABLE roles IS 'Stores roles that can be assigned to users.';
COMMENT ON COLUMN roles.id IS 'Primary key for the role, generated as a BIGINT.';
COMMENT ON COLUMN roles.name IS 'Unique name of the role.';
COMMENT ON COLUMN roles.description IS 'Description of the role.';
COMMENT ON COLUMN roles.inserted_at IS 'Timestamp when the role record was created.';
COMMENT ON COLUMN roles.updated_at IS 'Timestamp when the role record was last updated.';

-- DL-03: Create user_roles table
CREATE TABLE user_roles (
    user_id BIGINT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    role_id BIGINT NOT NULL REFERENCES roles(id) ON DELETE CASCADE,
    inserted_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
    PRIMARY KEY (user_id, role_id)
);
COMMENT ON TABLE user_roles IS 'Associates users with roles.';
COMMENT ON COLUMN user_roles.user_id IS 'Foreign key referencing the user.';
COMMENT ON COLUMN user_roles.role_id IS 'Foreign key referencing the role.';
COMMENT ON COLUMN user_roles.inserted_at IS 'Timestamp when the association was created.';
COMMENT ON COLUMN user_roles.updated_at IS 'Timestamp when the association was last updated.';

-- DL-04: Create sessions table
CREATE TABLE sessions (
    id BIGINT PRIMARY KEY,
    user_id BIGINT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    token TEXT NOT NULL UNIQUE,
    expires_at TIMESTAMP NOT NULL,
    inserted_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW()
);
COMMENT ON TABLE sessions IS 'Tracks user sessions for authentication.';
COMMENT ON COLUMN sessions.id IS 'Primary key for the session, generated as a BIGINT.';
COMMENT ON COLUMN sessions.user_id IS 'Foreign key referencing the user.';
COMMENT ON COLUMN sessions.token IS 'Unique token for the session.';
COMMENT ON COLUMN sessions.expires_at IS 'Timestamp when the session expires.';
COMMENT ON COLUMN sessions.inserted_at IS 'Timestamp when the session record was created.';
COMMENT ON COLUMN sessions.updated_at IS 'Timestamp when the session record was last updated.';

-- DL-05: Create devices table
CREATE TABLE devices (
    id BIGINT PRIMARY KEY,
    user_id BIGINT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    device_identifier TEXT NOT NULL UNIQUE,
    inserted_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW()
);
COMMENT ON TABLE devices IS 'Tracks devices associated with users.';
COMMENT ON COLUMN devices.id IS 'Primary key for the device, generated as a BIGINT.';
COMMENT ON COLUMN devices.user_id IS 'Foreign key referencing the user.';
COMMENT ON COLUMN devices.device_identifier IS 'Unique identifier for the device.';
COMMENT ON COLUMN devices.inserted_at IS 'Timestamp when the device record was created.';
COMMENT ON COLUMN devices.updated_at IS 'Timestamp when the device record was last updated.';

-- DL-06: Create login_attempts table
CREATE TABLE login_attempts (
    id BIGINT PRIMARY KEY,
    user_id BIGINT REFERENCES users(id) ON DELETE CASCADE,
    ip_address INET NOT NULL,
    succeeded BOOLEAN NOT NULL,
    attempted_at TIMESTAMP NOT NULL DEFAULT NOW(),
    inserted_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW()
);
COMMENT ON TABLE login_attempts IS 'Tracks login attempts for auditing and security.';
COMMENT ON COLUMN login_attempts.id IS 'Primary key for the login attempt, generated as a BIGINT.';
COMMENT ON COLUMN login_attempts.user_id IS 'Foreign key referencing the user.';
COMMENT ON COLUMN login_attempts.ip_address IS 'IP address from which the login attempt was made.';
COMMENT ON COLUMN login_attempts.succeeded IS 'Indicates whether the login attempt was successful.';
COMMENT ON COLUMN login_attempts.attempted_at IS 'Timestamp when the login attempt occurred.';
COMMENT ON COLUMN login_attempts.inserted_at IS 'Timestamp when the login attempt record was created.';
COMMENT ON COLUMN login_attempts.updated_at IS 'Timestamp when the login attempt record was last updated.';

-- DL-07: Create audit_logs table
CREATE TABLE audit_logs (
    id BIGINT PRIMARY KEY,
    user_id BIGINT REFERENCES users(id) ON DELETE SET NULL,
    action TEXT NOT NULL,
    details JSONB,
    performed_at TIMESTAMP NOT NULL DEFAULT NOW(),
    inserted_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW()
);
COMMENT ON TABLE audit_logs IS 'Stores audit logs for user actions.';
COMMENT ON COLUMN audit_logs.id IS 'Primary key for the audit log, generated as a BIGINT.';
COMMENT ON COLUMN audit_logs.user_id IS 'Foreign key referencing the user who performed the action.';
COMMENT ON COLUMN audit_logs.action IS 'Description of the action performed.';
COMMENT ON COLUMN audit_logs.details IS 'Additional details about the action in JSON format.';
COMMENT ON COLUMN audit_logs.performed_at IS 'Timestamp when the action was performed.';
COMMENT ON COLUMN audit_logs.inserted_at IS 'Timestamp when the audit log record was created.';
COMMENT ON COLUMN audit_logs.updated_at IS 'Timestamp when the audit log record was last updated.';

-- DL-08: Create rate_limits table
CREATE TABLE rate_limits (
    id BIGINT PRIMARY KEY,
    user_id BIGINT REFERENCES users(id) ON DELETE CASCADE,
    ip_address INET NOT NULL,
    bucket TEXT NOT NULL,
    count INT NOT NULL DEFAULT 0,
    reset_at TIMESTAMP NOT NULL,
    inserted_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW()
);
COMMENT ON TABLE rate_limits IS 'Tracks rate-limiting buckets for users and IPs.';
COMMENT ON COLUMN rate_limits.id IS 'Primary key for the rate limit, generated as a BIGINT.';
COMMENT ON COLUMN rate_limits.user_id IS 'Foreign key referencing the user.';
COMMENT ON COLUMN rate_limits.ip_address IS 'IP address associated with the rate limit.';
COMMENT ON COLUMN rate_limits.bucket IS 'Name of the rate-limiting bucket.';
COMMENT ON COLUMN rate_limits.count IS 'Current count of actions in the bucket.';
COMMENT ON COLUMN rate_limits.reset_at IS 'Timestamp when the rate limit resets.';
COMMENT ON COLUMN rate_limits.inserted_at IS 'Timestamp when the rate limit record was created.';
COMMENT ON COLUMN rate_limits.updated_at IS 'Timestamp when the rate limit record was last updated.';

-- DL-09: Create user_status_changes table
CREATE TABLE user_status_changes (
    id BIGINT PRIMARY KEY,
    user_id BIGINT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    old_status VARCHAR(50) NOT NULL,
    new_status VARCHAR(50) NOT NULL,
    changed_at TIMESTAMP NOT NULL DEFAULT NOW(),
    inserted_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW()
);
COMMENT ON TABLE user_status_changes IS 'Tracks changes to user statuses.';
COMMENT ON COLUMN user_status_changes.id IS 'Primary key for the status change, generated as a BIGINT.';
COMMENT ON COLUMN user_status_changes.user_id IS 'Foreign key referencing the user.';
COMMENT ON COLUMN user_status_changes.old_status IS 'Previous status of the user.';
COMMENT ON COLUMN user_status_changes.new_status IS 'New status of the user.';
COMMENT ON COLUMN user_status_changes.changed_at IS 'Timestamp when the status change occurred.';
COMMENT ON COLUMN user_status_changes.inserted_at IS 'Timestamp when the status change record was created.';
COMMENT ON COLUMN user_status_changes.updated_at IS 'Timestamp when the status change record was last updated.';

-- DL-10: Create user_events table
CREATE TABLE user_events (
    id BIGINT PRIMARY KEY,
    user_id BIGINT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    event_type TEXT NOT NULL,
    event_data JSONB,
    occurred_at TIMESTAMP NOT NULL DEFAULT NOW(),
    inserted_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW()
);
COMMENT ON TABLE user_events IS 'Tracks events related to users.';
COMMENT ON COLUMN user_events.id IS 'Primary key for the user event, generated as a BIGINT.';
COMMENT ON COLUMN user_events.user_id IS 'Foreign key referencing the user.';
COMMENT ON COLUMN user_events.event_type IS 'Type of the event (e.g., login, logout).';
COMMENT ON COLUMN user_events.event_data IS 'Additional data about the event in JSON format.';
COMMENT ON COLUMN user_events.occurred_at IS 'Timestamp when the event occurred.';
COMMENT ON COLUMN user_events.inserted_at IS 'Timestamp when the user event record was created.';
COMMENT ON COLUMN user_events.updated_at IS 'Timestamp when the user event record was last updated.';
