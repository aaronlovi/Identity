-------------------------------------------------------------------------------

create or replace function ${schema}.now_utc() returns timestamp as $$
   select now() at time zone 'utc';
$$ language sql;

-------------------------------------------------------------------------------

create table if not exists ${schema}.generator (
    last_reserved bigint not null
);
insert into ${schema}.generator (last_reserved) values (1);

COMMENT ON TABLE ${schema}.generator IS 'Tracks the last reserved ID for manual ID generation';
COMMENT ON COLUMN ${schema}.generator.last_reserved IS 'The last reserved ID for generating unique IDs';

-------------------------------------------------------------------------------

CREATE TABLE ${schema}.users (
    user_id BIGINT PRIMARY KEY,
    firebase_uid VARCHAR(128) UNIQUE NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL
);

COMMENT ON TABLE ${schema}.users IS 'Stores basic user information';
COMMENT ON COLUMN ${schema}.users.user_id IS 'Unique identifier for the user, generated externally';
COMMENT ON COLUMN ${schema}.users.firebase_uid IS 'Firebase UID for authentication mapping';
COMMENT ON COLUMN ${schema}.users.created_at IS 'Timestamp when the user was created';
COMMENT ON COLUMN ${schema}.users.updated_at IS 'Timestamp when the user was last updated';

-------------------------------------------------------------------------------

CREATE TABLE ${schema}.user_roles (
    user_id BIGINT NOT NULL,
    role VARCHAR(64) NOT NULL,
    assigned_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,
    PRIMARY KEY (user_id, role),
    FOREIGN KEY (user_id) REFERENCES ${schema}.users(user_id) ON DELETE CASCADE
);

COMMENT ON TABLE ${schema}.user_roles IS 'Stores roles assigned to users';
COMMENT ON COLUMN ${schema}.user_roles.user_id IS 'Reference to the user in the users table';
COMMENT ON COLUMN ${schema}.user_roles.role IS 'Role assigned to the user (e.g., admin, moderator)';
COMMENT ON COLUMN ${schema}.user_roles.assigned_at IS 'Timestamp when the role was assigned';

-------------------------------------------------------------------------------

CREATE TABLE ${schema}.user_status (
    user_id BIGINT PRIMARY KEY,
    status VARCHAR(32) NOT NULL,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,
    FOREIGN KEY (user_id) REFERENCES ${schema}.users(user_id) ON DELETE CASCADE
);

COMMENT ON TABLE ${schema}.user_status IS 'Tracks the current status of users';
COMMENT ON COLUMN ${schema}.user_status.user_id IS 'Reference to the user in the users table';
COMMENT ON COLUMN ${schema}.user_status.status IS 'Current status of the user (e.g., active, banned)';
COMMENT ON COLUMN ${schema}.user_status.updated_at IS 'Timestamp when the status was last updated';

-------------------------------------------------------------------------------
