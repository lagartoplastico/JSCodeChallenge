# How to run.
## Prerequisites
- Have a RabbitMQ Server.
- Have an Microsoft SQLServer DBMS.
## Steps
 - Clone the repository in a computer with the dotnet core sdk.
###  "JS.botStockProducer" Project Folder
- Update RabbitConnectionInfo in appsettings.json file.
- Execute the following commands in Powershell or a terminal inside this folder path:
   - dotnet build
   - dotnet run
###  "JS. web" Project Folder
- Update RabbitConnectionInfo in appsettings.json file to match your RabbitMQ Server. It must be the same as the before project.
- Update DefaultConnection in appsettings.json file to match your SQLServer in the following format: 
   - *Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword;*
- Execute the following commands in Powershell or a terminal inside this folder path:
   - dotnet build
   - dotnet ef database update
   - dotnet run
### Register, Login and Chat.

- Open a browser in https://localhost:5001
- Register a user with a fake email. Don't forget to click confirm email link.
- Login and click the tab named Chat.
- Do the same in other browser.
- Chat between browsers.
- You can send stock=stock_code commands.
