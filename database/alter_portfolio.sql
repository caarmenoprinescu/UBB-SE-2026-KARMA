USE KarmaBankingDb;

IF COL_LENGTH('Portfolio', 'userId') IS NOT NULL
BEGIN
    ALTER TABLE Portfolio
    DROP COLUMN userId;
END;

ALTER TABLE Portfolio
ADD userId INT NULL;
