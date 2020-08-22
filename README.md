## About
This application is written in C#.

It consists of two parts:
- WPF - client side text/code editor application
- ASP.NET MVC 4 - server side API code compiler and executor

### Supported languages
C++, Java, C#

## Requirements
- Microsoft Windows
- .NET runtime 4.5 or later
- SQL server
- Installed compilers for C++, Java, C# and added bin paths to environment variable

## Configuration
- Run `SQLServer database.sql`
- **(If necessary)** Change server `source` configuration to `source=your-sql-server-name;` in **`ProgrammingTasks/Web.config`** at:
```
<connectionStrings>
  <add name="programming_tasksEntities" connectionString="metadata=res://*/Models.Entity.EntityModel.csdl|res://*/Models.Entity.EntityModel.ssdl|res://*/Models.Entity.EntityModel.msl;provider=System.Data.SqlClient;provider connection string=&quot;data source=(local)\SQLEXPRESS;initial catalog=programming_tasks;integrated security=True;MultipleActiveResultSets=True;App=EntityFramework&quot;" providerName="System.Data.EntityClient"/>
<connectionStrings>
```
