@echo off

REM 1. Register a user
echo Registering testuser
curl -X POST http://localhost:12000/users -H "Content-Type: application/json" -d "{\"username\": \"testuser\", \"password\": \"1234\"}"
echo.
echo.

REM 2. Login a user
echo Logging in testuser
curl -X POST http://localhost:12000/login -H "Content-Type: application/json" -d "{\"username\": \"testuser\", \"password\": \"1234\"}"
echo.
echo.

REM Prompt for token
echo Copy the token from above and paste it here:
set /p TOKEN=Token:
echo.

REM 3. Profile stats
echo Get profile stats
curl -X GET http://localhost:12000/users/testuser/profile -H "Authorization: Bearer %TOKEN%"
echo.
echo.

REM 4. Create new media entry
echo Creating media entry
curl -X POST http://localhost:12000/media -H "Content-Type: application/json" -H "Authorization: Bearer %TOKEN%" -d "{\"title\": \"Silksong\", \"description\": \"Discover a vast, haunted kingdom in Hollow Knight: Silksong! Explore, fight and survive as you ascend to the peak of a land ruled by silk and song.\", \"type\": \"Game\", \"release_year\": 2025, \"age_restriction\": 10}"
echo.
echo.

REM 5. List media
echo List all media
curl -X GET http://localhost:12000/media -H "Authorization: Bearer %TOKEN%"
echo.
echo.

REM Prompt for media id to get media details
echo Enter the media id from one of the listed media entries above
set /p MID=Media ID:

REM 6. Media details
echo Get media details
curl -X GET http://localhost:12000/media/%MID% -H "Authorization: Bearer %TOKEN%"
echo.
echo.

REM 7. Update media
echo Updating media description
curl -X PUT http://localhost:12000/media/%MID% -H "Content-Type: application/json" -H "Authorization: Bearer %TOKEN%" -d "{\"description\": \"Updated description\"}"
echo.
echo.

REM REPEAT. Media details
echo Get media details
curl -X GET http://localhost:12000/media/%MID% -H "Authorization: Bearer %TOKEN%"
echo.
echo.

REM 8. Delete media
echo Deleting media
curl -X DELETE http://localhost:12000/media/%MID% -H "Authorization: Bearer %TOKEN%"
echo.
echo.

REM REPEAT. Media details
echo Get media details
curl -X GET http://localhost:12000/media/%MID% -H "Authorization: Bearer %TOKEN%"
echo.
echo.

REM 9. Delete user
echo Deleting testuser
curl -X DELETE http://localhost:12000/users/testuser -H "Authorization: Bearer %TOKEN%"
echo.
echo.

@pause