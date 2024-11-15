CREATE TABLE `Users` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `Name` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL DEFAULT '',

  PRIMARY KEY (`Id`)



);


CREATE TABLE `Projects` (
  `ProjectID` bigint NOT NULL AUTO_INCREMENT,
  `Id` char(36) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `UserId` bigint NOT NULL,
  `Name` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL DEFAULT '',
  PRIMARY KEY (`ProjectID`),
  KEY `UserId` (`UserId`),
  CONSTRAINT `projects_users_ibfk_1` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`)
) 
