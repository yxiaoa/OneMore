﻿CREATE TABLE IF NOT EXISTS hashtag_scanner (scannerID INTEGER PRIMARY KEY UNIQUE NOT NULL, version NUMERIC (12) UNIQUE NOT NULL, lastScan TEXT NOT NULL);
CREATE TABLE IF NOT EXISTS hashtags (tag TEXT NOT NULL, moreID TEXT NOT NULL, pageID TEXT NOT NULL, objectID TEXT NOT NULL, lastScan TEXT NOT NULL, PRIMARY KEY (tag, objectID));
CREATE TABLE IF NOT EXISTS hashtags_pages (moreID PRIMARY KEY, path TEXT, name TEXT);
CREATE INDEX IF NOT EXISTS IDX_moreID ON hashtags (moreID);
CREATE INDEX IF NOT EXISTS IDX_pageID ON hashtags (pageID);
CREATE INDEX IF NOT EXISTS IDX_tag ON hashtags (tag);
REPLACE INTO hashtag_scanner (scannerID, version, lastScan) VALUES (0, 1,'0001-01-01T00:00:00.0000Z');