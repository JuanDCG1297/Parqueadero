@echo off
REM ============================================
REM  Comandos útiles para MySQL en Docker
REM  Ejecutar en terminal como administrador
REM ============================================

:MENU
cls
echo ===== PARQUEADERO — MySQL Docker =====
echo.
echo  1. Ver todos los vehículos
echo  2. Ver solo vehículos estacionados
echo  3. Ver vehículos que ya salieron
echo  4. Contar registros
echo  5. Borrar todos los datos
echo  6. Entrar a consola MySQL
echo  7. Salir
echo.
set /p opcion="Seleccioná una opción: "

if "%opcion%"=="1" goto VER_TODOS
if "%opcion%"=="2" goto VER_ACTIVOS
if "%opcion%"=="3" goto VER_SALIDAS
if "%opcion%"=="4" goto CONTAR
if "%opcion%"=="5" goto LIMPIAR
if "%opcion%"=="6" goto CONSOLA
if "%opcion%"=="7" goto SALIR
goto MENU

:VER_TODOS
docker exec -it parking-mysql mysql -uroot -proot -e "USE parqueadero; SELECT Id, Plate, VehicleType, EntryTime, ExitTime, TotalMinutes, Fee, EmailSent FROM VehicleEntries\G"
pause
goto MENU

:VER_ACTIVOS
docker exec -it parking-mysql mysql -uroot -proot -e "USE parqueadero; SELECT Id, Plate, VehicleType, EntryTime FROM VehicleEntries WHERE ExitTime IS NULL;"
pause
goto MENU

:VER_SALIDAS
docker exec -it parking-mysql mysql -uroot -proot -e "USE parqueadero; SELECT Id, Plate, EntryTime, ExitTime, TotalMinutes, Fee FROM VehicleEntries WHERE ExitTime IS NOT NULL;"
pause
goto MENU

:CONTAR
docker exec -it parking-mysql mysql -uroot -proot -e "USE parqueadero; SELECT COUNT(*) AS Total FROM VehicleEntries; SELECT COUNT(*) AS Estacionados FROM VehicleEntries WHERE ExitTime IS NULL;"
pause
goto MENU

:LIMPIAR
echo.
echo ⚠️  Esto va a borrar TODOS los datos!
set /p confirm="Estás seguro? (s/n): "
if "%confirm%"=="s" docker exec -it parking-mysql mysql -uroot -proot -e "USE parqueadero; DELETE FROM VehicleEntries;"
if "%confirm%"=="s" echo ✅ Datos borrados!
pause
goto MENU

:CONSOLA
echo.
echo 📝 Escribí tus consultas SQL. Para salir: exit
echo.
docker exec -it parking-mysql mysql -uroot -proot parqueadero
goto MENU

:SALIR
echo.
echo 👋 Chau!
