@echo off

REM Register a new user to delete as admin later
echo Registering testuser
curl -X POST http://localhost:12000/users -H "Content-Type: application/json" -d "{\"username\": \"testuser\", \"password\": \"1234\"}"
echo.
echo.

REM Admin Setup
echo Admin Setup
curl -X POST http://localhost:12000/users -H "Content-Type: application/json" -d "{\"username\":\"admin\",\"password\":\"admin\"}"
echo.
echo.

REM Admin Login
echo Admin Login
curl -X POST http://localhost:12000/login -H "Content-Type: application/json" -d "{\"username\":\"admin\",\"password\":\"admin\"}"
echo.
echo.

REM Prompt for token
echo Copy the token from above and paste it here:
set /p TOKEN=Token:

REM Prompt for user to delete
echo Enter the username of the user to be deleted:
set /p USERNAME=Username: 

REM Deleting a user as admin
echo Deleting a user as admin
curl -X DELETE http://localhost:12000/users/%USERNAME% -H "Authorization: Bearer %TOKEN%"
echo.

@pause