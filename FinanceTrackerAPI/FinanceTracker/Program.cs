using FinanceTracker;

//configuration and dependency injection setup for the web application.
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<TransactionManager>();//create an instance of TransactionManager so it can be injected into the endpoints
builder.Services.AddSingleton<FileStorage>();
builder.Services.AddCors(o=> o.AddDefaultPolicy(p=> p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));//allow front end to access the api from any origin. to change after development, specify the allowed origins instead of AllowAnyOrigin()
builder.Services.AddEndpointsApiExplorer();//add support for API documentation generation
builder.Services.AddSwaggerGen();

var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";

builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var app = builder.Build();//build the application
app.UseCors();//enable CORS for the application
app.UseSwagger();//
app.UseSwaggerUI();//enable the Swagger UI for testing the API endpoints and viewing documentation. this will be available at /swagger in the browser


var manager= app.Services.GetRequiredService<TransactionManager>();//grabs teh singleton instance of TransactionManager from the dependency injection container so it can be used in the endpoints to manage transactions
var storage= app.Services.GetRequiredService<FileStorage>();

manager.Load(storage.LoadData());//load transactions from the data file into the TransactionManager when the application starts

app.MapGet("/api/transactions", (TransactionManager mgr)=> Results.Ok(mgr.Transactions));//this endpoint returns all transactions as a JSON array when a GET request is made to /api/transactions, and gives a 200 OK status code

app.MapPost("/api/transactions",(CreateTransactionRequest req, TransactionManager mgr,FileStorage fs) =>
{
    if (string.IsNullOrEmpty(req.Type) || (req.Type!= "expense" && req.Type != "income"))//validate the type. it must be either "expense" or "income"
    {
        return Results.BadRequest("Type must be income or expense");//returns a 400 Bad Request status code with an error message if the type is invalid
    }

    if(req.Amount <= 0)//validate the amount. it must be a positive number. as it will be stored as negative for expenses and positive for income
    {
        return Results.BadRequest("Amount must be positive");
    }

    var t = new Transaction
    {
        Type = req.Type,
        Category = req.Category ?? "generic",//if category is null, set it to "generic"
        Amount = req.Type == "expense" ? -Math.Abs(req.Amount) : Math.Abs(req.Amount),
        Date = req.Date ?? DateTime.Now,//if date is null, set it to the current date and time
        Note = req.Note ?? ""
    };

    mgr.Add(t);//add the new transaction

    fs.SaveData(mgr.Transactions.ToList());//save the updated transactions list to the data file

    return Results.Created($"/api/transaction/{t.Id}",t);

});//this endpoint adds a transaction if valid

app.MapDelete("/api/transactions/last", (TransactionManager mgr, FileStorage fs) =>
{
    if(!mgr.Transactions.Any()) return Results.BadRequest("No transactions to remove");

    var last= mgr.Transactions.Last();

    mgr.Remove(last.Id);

    fs.SaveData(mgr.Transactions.ToList());
    return Results.Ok(new {message=$"Last transaction removed", removed = last});

});//this endpoint deletes the last transaction

app.MapDelete("/api/transactions/{id:int}", (int id, TransactionManager mgr, FileStorage fs) =>
{
    var t = mgr.GetById(id);//get the transaction by id

    if (t is null) return Results.NotFound($"Transaction {id} not found");//if the transaction doesn't exist, return a 404 Not Found status code with an error message

    mgr.Remove(id);//remove the transaction

    fs.SaveData(mgr.Transactions.ToList());//save the updated transactions list to the data file

    return Results.Ok(new { message="Transaction deleted" });
});//this endpoint deletes a transaction if it exists

app.MapPut("/api/transactions/{id:int}", (int id, UpdateTransactionRequest req, TransactionManager mgr, FileStorage fs) =>
{
    var t= mgr.GetById(id);

    if (t is null) return Results.NotFound($"Transaction {id} not found");

    if (!string.IsNullOrEmpty(req.Type))
    {
        t.Type= req.Type;
        t.Amount= req.Type=="expense"? -Math.Abs(t.Amount): Math.Abs(t.Amount);
    }
    if(!string.IsNullOrEmpty(req.Category)) t.Category= req.Category;
    if (req.Amount.HasValue) t.Amount = req.Type == "expense" ? -Math.Abs(req.Amount.Value) : Math.Abs(req.Amount.Value);
    if(req.Date.HasValue) t.Date= req.Date.Value;
    if(req.Note is not null) t.Note= req.Note;

    fs.SaveData(mgr.Transactions.ToList());

    return Results.Ok(new {message=$"Transaction {id} updated"});
});//this endpoint changes a transaction if it exists

app.MapGet("/api/transactions/summary", (TransactionManager mgr) =>
{
    var all = mgr.Transactions;

    return Results.Ok(new
    {
        balance = all.Sum(t => t.Amount),
        totalIncome = all.Where(t => t.Type == "income").Sum(t => t.Amount),
        totalExpense = all.Where(t => t.Type == "expense").Sum(t => t.Amount),
        count = all.Count
    });

});//this endpoint returns a summary


app.Run();//start the application and listen for incoming requests

record CreateTransactionRequest(string Type, string? Category, decimal Amount, DateTime? Date, string? Note);
record UpdateTransactionRequest(string? Type, string? Category, decimal? Amount, DateTime? Date, string? Note);