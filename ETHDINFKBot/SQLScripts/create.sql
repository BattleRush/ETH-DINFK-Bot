-- Database: employee

DROP DATABASE IF EXISTS employee WITH (FORCE);

CREATE DATABASE employee
    WITH 
    OWNER = postgres
    ENCODING = 'UTF8'
    LC_COLLATE = 'English_Switzerland.1252'
    LC_CTYPE = 'English_Switzerland.1252'
    TABLESPACE = pg_default
    CONNECTION LIMIT = -1;



-- Database: tpch

DROP DATABASE IF EXISTS tpch WITH (FORCE);

CREATE DATABASE tpch
    WITH 
    OWNER = postgres
    ENCODING = 'UTF8'
    LC_COLLATE = 'English_Switzerland.1252'
    LC_CTYPE = 'English_Switzerland.1252'
    TABLESPACE = pg_default
    CONNECTION LIMIT = -1;

    

-- Database: zvv

DROP DATABASE IF EXISTS zvv WITH (FORCE);

CREATE DATABASE zvv
    WITH 
    OWNER = postgres
    ENCODING = 'UTF8'
    LC_COLLATE = 'English_Switzerland.1252'
    LC_CTYPE = 'English_Switzerland.1252'
    TABLESPACE = pg_default
    CONNECTION LIMIT = -1;

DROP OWNED BY {0};
--REASSIGN OWNED BY {0} TO postgres;  -- or some other trusted role
DROP USER {0};

CREATE ROLE {0} WITH
  LOGIN
  NOSUPERUSER
  INHERIT
  NOCREATEDB
  NOCREATEROLE
  NOREPLICATION
  PASSWORD '{1}';


--GRANT SELECT, UPDATE, INSERT, DELETE, TRUNCATE, REFERENCES, TRIGGER ON ALL TABLES IN SCHEMA public TO {0};
GRANT ALL PRIVILEGES ON DATABASE employee TO {0};
GRANT ALL PRIVILEGES ON DATABASE zvv TO {0};
GRANT ALL PRIVILEGES ON DATABASE tpch TO {0};