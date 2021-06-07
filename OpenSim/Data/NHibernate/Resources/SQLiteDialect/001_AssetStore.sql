CREATE TABLE Assets (
  ID VARCHAR(36) NOT NULL,
  Type SMALLINT DEFAULT NULL,
  Name VARCHAR(64) DEFAULT NULL,
  Description VARCHAR(64) DEFAULT NULL,
  Local BIT DEFAULT NULL,
  Temporary BIT DEFAULT NULL,
  Data BLOB,
  PRIMARY KEY (ID)
);
