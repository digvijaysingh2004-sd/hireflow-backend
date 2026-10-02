-- Automatically create the remaining microservice databases when PostgreSQL starts
SELECT 'CREATE DATABASE hireflow_hiring'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'hireflow_hiring')\gexec

SELECT 'CREATE DATABASE hireflow_notification'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'hireflow_notification')\gexec
