# IntelligentData

This is an extension library for EntityFramework that adds some intelligence to entities and contexts.


## Features

* __Access Control__    
  The ability to limit insert, update, and delete activity in the DB context.
* __String Formatting__    
  String formats can easily be applied to properties when they are saved.
* __Runtime Defaults for Properties__    
  Provide default values for properties based on runtime execution (eg - current date/time).
* __Auto-update Properties__  
  Generates a new value for a property automatically when an entity is saved (eg - current date/time).
* __Tracking Interfaces__  
  Building on the auto-update and runtime defaults, there are ITrackedEntity interfaces available
  that automatically setup specific properties to track date/time and user information.
* __Concurrent Versioned Interfaces__  
  Define entities with 64-bit version tracking properties just by implementing the interface.
  The context takes care of updating the property on save and tracking the original value for
  concurrency checking.
* __Intelligent Entities__  
  Entities tied to a DB context allow the entity reference to accomplish many data management tasks.
  For instance, the entity can be saved or deleted directly without interfacing with the DB context.
  The entity also has access to the DB context for custom lazy loading.
* __GetSqlString and TryGetSqlString__  
  Allows the retrieval of SQL text from a Queryable object.  In terms of functionality, this is
  more of an aesthetic extension.  However, this makes it possible to create bulk actions against
  the database by having the SQL in an easy to use format.  It also makes it possible to double
  check other functionality as it is added to the library.
* __Table Name Prefix__  
  Setting a table name prefix on an IntelligentDbContext will cause all table names to be prefixed
  with that value.  In a shared database, this would allow you to easily segregate different data
  without worrying about naming conflicts.
* __Entity Update Commands__  
  Allows easily manipulating the database contents without the use of the context change tracker.
  Primarily this is useful within a transaction when you have many changes to make.  The default
  behavior of a DbContext is to submit all changes at once when you call SaveChanges().
  This method generates a massive set of SQL commands to send to the server that may take an
  excessive amount of time to complete and may even timeout.  By using entity update commands
  within a transaction involving thousands of database updates, the commands are queued up inside
  the database server as they are encountered and only committed if the overall operation is 
  successful.  The database server doesn't have to parse a massive set of commands all at once
  and a timeout is far less likely to occur.
* __Parameterized SQL__  
  Entity queries can be converted into parameterized SQL objects.  Queries for entity types can
  then easily be converted into UPDATE or DELETE statements for bulk operations.  Parameterized
  SQL objects can be easily converted into FormattableStrings or executed against the original
  DbContext.
 

## Usage

```sh
dotnet add package IntelligentData
```

```c#

// Allow entities to be created and updated, but not deleted.
[Access(AccessLevel.Insert | AccessLevel.Update)]
public class MyEntity : IntelligentEntity<MyDbContext>, IVersionedEntity
{
    public MyEntity(MyDbContext context) : base(context)
    {
    }
    
    [Key]
    public int ID { get; set; }

    // Store the user name in upper case.
    [UpperCase]
    public string UserName { get; set; }
    
    // Set a timestamp when the record is created.
    [RuntimeDefaultNow]
    public DateTime Created { get; set; }
    
    // Set a timestamp when the record is modified.
    [AutoUpdateToNow]
    public DateTime LastModified { get; set; }

    public int SaveCount { get; set; }

    [RuntimeDefaultCurrentUserName]
    public string CreatedBy { get; set; }

    [AutoUpdateToCurrentUserName]
    public string LastModifiedBy { get; set; }

    // handled by the context, we never need to look at this value in our code.
    public long? RowVersion { get; set; }
}

public class MyDbContext : IntelligentDbContext
{
    public MyDbContext(
        DbContextOptions<MyDbContext> options,              // options to build the DbContext
        IUserInformationProvider userInformationProvider    // user information provider for user tracking
    )
        : base(options, userInformationProvider)
    {
    }

    public DbSet<MyEntity> MyEntities { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        // add a custom delegate as the auto-update provider.
        builder
            .Entity<MyEntity>()
            .Property(x => x.SaveCount)
            .HasAutoUpdate(v => (int)v + 1);
    }
}

var context = new MyDbContext(...);
var entity = new MyEntity(context)
{
    UserName = "John.Doe"
};
entity.SaveToDatabase();

```




## License

Licensed under the [MIT License](https://opensource.org/licenses/MIT).

Copyright (C) 2020 Beau Barker

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
