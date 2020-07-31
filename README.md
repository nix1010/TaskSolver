## About
This application is written entirely in C#.

It consists of two parts:
- WPF - client side text/code editor application
- ASP.NET - server side API code compiler and executor

## Requirements
- Microsoft Windows
- .NET runtime 4.5 or later
- SQL server
- Installed compilers for C++, Java, C# and added bin paths to environment variable

## Configuration
- Run SQLServer database.sql
- Change server configuration `source=your-sql-server-name;` in **ProgrammingTasks/Web.config** at:
```
  <add name="DBEntities" connectionString="metadata=res://*/Models.Entity.EntityModel.csdl|res://*/Models.Entity.EntityModel.ssdl|res://*/Models.Entity.EntityModel.msl;provider=System.Data.SqlClient;provider connection string=&quot;data source=your-sql-server-name;initial catalog=programming_tasks;integrated security=True;MultipleActiveResultSets=True;App=EntityFramework&quot;" providerName="System.Data.EntityClient" />
```
