DO
$$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_database
        WHERE datname = 'music_service'
    ) THEN
        CREATE DATABASE music_service;
    END IF;
END
$$;
