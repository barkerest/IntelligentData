# IntelligentData

This is an extension library for EntityFramework that adds some intelligence to models and contexts.


## Features

* __Access Control__    
  The ability to limit insert, update, and delete activity in the DB context.
* __String Formatting__    
  String formats can easily be applied to properties when they are saved.
* __Runtime Defaults for Properties__    
  Provide default values for properties based on runtime execution (eg - current date/time).
* __Auto-update Properties__  
  Generates a new value for a property automatically when an entity is saved (eg - current date/time).

## Usage

```sh
dotnet add package IntelligentData
```

```c#

// Allow entities to be created and updated, but not deleted.
[Access(AccessLevel.Insert | AccessLevel.Update)]
public class MyEntity
{
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
}

public class MyDbContext : IntelligentDbContext
{
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
```




## License

Licensed under the [MIT License](https://opensource.org/licenses/MIT).

Copyright (C) 2020 Beau Barker

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
