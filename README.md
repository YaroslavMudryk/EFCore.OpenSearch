# OpenSearch EntityFrameworkCore Provider

[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)
[![OpenSearch](https://img.shields.io/badge/OpenSearch-Supported-blue)](https://opensearch.org/)
[![.NET](https://img.shields.io/badge/.NET-6.0%2B-blue)](https://dotnet.microsoft.com/)

## 🌟 Overview

This package integrates **OpenSearch** with **Entity Framework Core**, allowing you to use `DbContext` API with OpenSearch.  
It supports **LINQ queries**, **CRUD operations**, **full-text search**, **custom `QueryContainer` injection**, and more!

---

## 🚀 **Features**
✅ **LINQ support** (`Where()`, `Select()`, `OrderBy()`, `Skip()`, `Take()`)  
✅ **CRUD operations** (`Add()`, `Update()`, `Remove()`, `SaveChanges()`)  
✅ **Asynchronous queries** (`ToListAsync()`, `FirstOrDefaultAsync()`, `CountAsync()`)  
✅ **Full-text search** (`MatchQuery`, `MultiMatchQuery`, `BoolQuery`)  
✅ **Custom OpenSearch Queries (`QueryContainer`) injection**  
✅ **Track total hits** (`Count()` and `LongCount()` enable `TrackTotalHits = true`)  
✅ **Fluent API for index configuration**  

---

## 📦 **Installation**
Add the package to your .NET project:

```sh
dotnet add package OpenSearch.EntityFrameworkCore
```

---

## ⚙️ **Configuration**
To use OpenSearch as the EF Core provider, configure your `DbContext`:

```csharp
var client = new OpenSearchClient(new ConnectionSettings(new Uri("http://localhost:9200"))
    .DefaultIndex("my_index"));

var optionsBuilder = new DbContextOptionsBuilder<MyOpenSearchDbContext>();
optionsBuilder.UseOpenSearch(client, "my_index");

var context = new MyOpenSearchDbContext(optionsBuilder.Options);
```

Alternatively, in **ASP.NET Core**:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Register OpenSearch client in DI
builder.Services.AddSingleton(new OpenSearchClient(new ConnectionSettings(new Uri("http://localhost:9200"))
    .DefaultIndex("my_index")));

// Register DbContext
builder.Services.AddDbContext<MyOpenSearchDbContext>((serviceProvider, options) =>
{
    var client = serviceProvider.GetRequiredService<OpenSearchClient>();
    options.UseOpenSearch(client, "my_index");
});

var app = builder.Build();
```

---

## 🏗 **Defining Your `DbContext`**
Create your custom `DbContext` that extends `OpenSearchDbContext`:

```csharp
public class MyOpenSearchDbContext : OpenSearchDbContext
{
    public MyOpenSearchDbContext(DbContextOptions<MyOpenSearchDbContext> options)
        : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().ToOpenSearchIndex("users_index");
        modelBuilder.Entity<Order>().ToOpenSearchIndex("orders_index");

        base.OnModelCreating(modelBuilder);
    }
}
```

---

## 🔍 **Usage Examples**

### **Basic LINQ Query**
```csharp
var users = await context.Users
    .Where(u => u.Age > 18)
    .OrderBy(u => u.Name)
    .Take(10)
    .ToListAsync();
```

### **Full-Text Search**
```csharp
var searchQuery = new MatchQuery { Field = "name", Query = "John Doe" };

var users = await context.Users
    .WithQuery(searchQuery) // Injecting OpenSearch QueryContainer
    .ToListAsync();
```

### **Inserting Data**
```csharp
var newUser = new User { Id = 1, Name = "John Doe", Age = 30 };
context.Users.Add(newUser);
await context.SaveChangesAsync();
```

### **Updating Data**
```csharp
var user = await context.Users.FirstOrDefaultAsync(u => u.Id == 1);
user.Name = "John Updated";
await context.SaveChangesAsync();
```

### **Deleting Data**
```csharp
var user = await context.Users.FirstOrDefaultAsync(u => u.Id == 1);
context.Users.Remove(user);
await context.SaveChangesAsync();
```

### **Counting Documents (`TrackTotalHits = true`)**
```csharp
var totalUsers = await context.Users.CountAsync();
Console.WriteLine($"Total users: {totalUsers}");
```

### **Using a Custom OpenSearch Query (`QueryContainer`)**
```csharp
var query = new BoolQuery
{
    Must = new List<QueryContainer>
    {
        new MatchQuery { Field = "name", Query = "John" },
        new RangeQuery { Field = "age", GreaterThan = 18 }
    }
};

var users = await context.Users
    .WithQuery(query) // Injecting custom OpenSearch Query
    .ToListAsync();
```

---

## 🏗 **Roadmap**
🚀 Bulk operations (`BulkInsert`, `BulkUpdate`)  
🚀 Index auto-migration (`EnsureIndexCreated()`)  
🚀 OpenSearch aggregations support (`.GroupBy()`)  
🚀 Better error handling & logging  

---

## ⚖️ **License**
This project is licensed under the **MIT License**.

---

## 🤝 **Contributing**
Contributions are welcome! Feel free to submit issues and pull requests.

---

## 📫 **Contact**
For support, reach out via [GitHub Issues](https://github.com/your-repo/issues).

---

## 🎯 **Final Thoughts**
Now you can use OpenSearch **just like SQL** with Entity Framework Core, while still having access to full-text search and complex OpenSearch queries! 🚀

**Star ⭐ this repository if you found it useful!**

