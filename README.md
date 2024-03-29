﻿# IntelligentData

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
  DbContext.  Extension methods are provided to allow BulkUpdate and BulkDelete to be called
  against an entity query.
* __IEntityCustomizer__  
  Attributes defined with this interface will allow for runtime customization of the data model
  in an IntelligentDbContext.
* __IndexAttribute__ and __CompositeIndexAttribute__  
  These attributes are examples of the IEntityCustomizer interface and also provide model validation
  if the Unique property is set to true.  They also make use of extensions added to the 
  ValidationContext type to allow extracting the appropriate DbContext for an object being 
  validated and for generating basic SQL count statements.
* __Temporary Lists__  
  Often times I found myself having multiple somewhat related databases where IDs might be shared.
  In these cases EF would insert those values into the SQL statement generated, which could lead to
  excessively huge SQL statements.  Worse, I might need to perform multiple transactions with the
  list which would cause more giant SQL statements.  To include the temporary tables in your model
  you will need to call the `WithTemporaryLists()` extension method on your DbContextOptions builder.


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

var context = new MyDbContext(
    new DbContextOptionsBuilder<MyDbContext>()
        .WithTemporaryLists()
        .Options,
    ACurrentUserAccessorObject);
var entity = new MyEntity(context)
{
    UserName = "John.Doe"
};
entity.SaveToDatabase();

```

Check out the tests for examples of how the various features are supposed to work.

## Version History

* __6.0.5__ 2023-08-17  
  Added ThrowOnAccessLevelViolation property to IntelligentDbContext.  
  When set to true, SaveChanges() will throw a DbUpdateException when a violation is encountered instead of silently
  removing the violating entity from the change tracker.
* __6.0.4.2__ 2022-03-25  
  Fix EntityUpdateCommands to use transaction connection when provided.  
  Fix EntityUpdateCommands to only exclude explicitly provided key properties on insert if they were automatically
  included.

* __6.0.3__ 2022-03-17  
  Fixed bugs related to ParameterizedSql generation.

* __6.0.2__ 2022-03-16  
  Fix bugs related to closed connections in EntityUpdateCommands.  
  Update the testing to be able to test MySql and SqlServer in addition to Sqlite.

* __6.0.1__ 2022-02-10  
  Fix minor typos in the readme file.

* __6.0.0__ 2022-02-10  
  Update to work with EF Core 6.0.

* __1.2.7__ 2021-01-21  
  Fix temporary list missing parameters.

* __1.2.6__ 2021-01-13  
  Fix null reference exception on shadow properties.
  
* __1.2.5__ 2021-01-12  
  Allow for blank table name prefix (null, empty, or whitespace) to be ignored.

* __1.2.4__ 2020-05-26  
  Add transaction parameter to BulkDelete and BulkUpdate.

* __1.2.3__ 2020-04-23  
  Add warnings when entities are removed from the change tracker.  
  Made DefaultAccessLevel abstract to force setting a value in implementations. 

* __1.2.2__ 2020-04-16  
  Made the attribute scanning include inherited attributes within the IntelligentDbContext.

* __1.2.1__ 2020-04-15  
  Made temporary lists default to not included to prevent screwing up migrations.

* __1.2.0__ 2020-04-15  
  Added temporary lists.

* __1.1.0__  2020-04-02  
  Added IEntityCustomizer, IndexAttribute, and CompositeIndexAttribute.

* __1.0.0__  2020-03-27  
  The initial release.

## License

Licensed under the [MIT License](https://opensource.org/licenses/MIT).

Copyright (C) 2020-2023 Beau Barker

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
