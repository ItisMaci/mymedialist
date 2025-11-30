@echo off

curl -X POST http://localhost:12000/login -H "Content-Type: application/json" -d "{\"username\":\"admin\",\"password\":\"admin\"}"

REM Prompt for token
echo Copy the token from above and paste it here:
set /p TOKEN=Token:

curl -X POST http://localhost:12000/media -H "Authorization: Bearer %TOKEN%" -H "Content-Type: application/json" -d "{\"title\":\"Chainsaw-Man Movie: Reze\",\"description\":\"Story Arc Reze based of the Manga.\",\"type\":\"Movie\",\"release_year\":2025,\"age_restriction\":14}"

REM Prompt for media id to get media details
echo Enter the media id from one of the listed media entries above
set /p MID=Media ID:

REM Media details
echo Get media details
curl -X GET http://localhost:12000/media/%MID% -H "Authorization: Bearer %TOKEN%"
echo.
echo.
@pause