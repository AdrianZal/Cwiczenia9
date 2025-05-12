using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Tutorial9.Models;

namespace Tutorial9.Services;

public class WarehouseService : IWarehouseService
{
    private readonly string _connectionString =
        "Data Source=db-mssql;Initial Catalog=2019SBD;Integrated Security=True;Trust Server Certificate=True";
    
    public async Task<int> AddEntry(Entry entry)
    {
        if (entry.idProduct==null)
            throw new ArgumentException("Product ID is required");
        if (entry.idWarehouse==null)
            throw new ArgumentException("Warehouse ID is required");
        if (entry.amount==null || entry.amount <= 0)
            throw new ArgumentException("Amount is required");
        if (entry.createdAt==null)
            throw new ArgumentException("Creation date is required");

        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            await conn.OpenAsync();
            using (SqlTransaction transaction = conn.BeginTransaction())
            {
                try
                {
                    //Pkt. 1
                    var productCmd = new SqlCommand("SELECT 1 FROM Product WHERE IdProduct = @IdProduct", conn, transaction);
                    productCmd.Parameters.AddWithValue("@IdProduct", entry.idProduct);
                    if (await productCmd.ExecuteScalarAsync() == null)
                        throw new Exception("Product not found");
                    
                    var warehouseCmd = new SqlCommand("SELECT 1 FROM Warehouse WHERE IdWarehouse = @IdWarehouse", conn, transaction);
                    warehouseCmd.Parameters.AddWithValue("@IdWarehouse", entry.idWarehouse);
                    if (await warehouseCmd.ExecuteScalarAsync() == null)
                        throw new Exception("Warehouse not found");
                    
                    //Pkt. 2
                    var orderCmd = new SqlCommand(@"
                        SELECT IdOrder FROM [Order]
                        WHERE IdProduct = @IdProduct AND Amount = @Amount AND CreatedAt < @CreatedAt", conn, transaction);
                    orderCmd.Parameters.AddWithValue("@IdProduct", entry.idProduct);
                    orderCmd.Parameters.AddWithValue("@Amount", entry.amount);
                    orderCmd.Parameters.AddWithValue("@CreatedAt", entry.createdAt);
                    var orderIdObj = await orderCmd.ExecuteScalarAsync();
                    if (orderIdObj == null)
                        throw new Exception("Matching order not found");
                    
                    //Pkt. 3
                    int orderId = (int)await orderCmd.ExecuteScalarAsync();
                    var checkExistingCmd = new SqlCommand("SELECT 1 FROM Product_Warehouse WHERE IdOrder = @IdOrder", conn, transaction);
                    checkExistingCmd.Parameters.AddWithValue("@IdOrder", orderId);
                    if (await checkExistingCmd.ExecuteScalarAsync() != null)
                        throw new Exception("Order already fulfilled");
                    
                    //Pkt. 4
                    var updateOrderCmd = new SqlCommand("UPDATE [Order] SET FulfilledAt = GETDATE() WHERE IdOrder = @IdOrder", conn, transaction);
                    updateOrderCmd.Parameters.AddWithValue("@IdOrder", orderId);
                    await updateOrderCmd.ExecuteNonQueryAsync();
                    
                    //Pkt. 5
                    var priceCmd = new SqlCommand("SELECT Price FROM Product WHERE IdProduct = @IdProduct", conn, transaction);
                    priceCmd.Parameters.AddWithValue("@IdProduct", entry.idProduct);
                    var priceObj = await priceCmd.ExecuteScalarAsync();
                    if (priceObj == null)
                        throw new Exception("Product price not found");
                    decimal unitPrice = (decimal)priceObj;
                    
                    var insertCmd = new SqlCommand(@"
                        INSERT INTO Product_Warehouse(IdWarehouse, IdProduct, IdOrder, Amount, Price, CreatedAt)
                        OUTPUT INSERTED.IdProductWarehouse
                        VALUES(@IdWarehouse, @IdProduct, @IdOrder, @Amount, @TotalPrice, @CreatedAt)", conn, transaction);
                    insertCmd.Parameters.AddWithValue("@IdWarehouse", entry.idWarehouse);
                    insertCmd.Parameters.AddWithValue("@IdProduct", entry.idProduct);
                    insertCmd.Parameters.AddWithValue("@IdOrder", orderId);
                    insertCmd.Parameters.AddWithValue("@Amount", entry.amount);
                    insertCmd.Parameters.AddWithValue("@TotalPrice", entry.amount * unitPrice);
                    insertCmd.Parameters.AddWithValue("@CreatedAt", entry.createdAt);
                    
                    int newId = (int)await insertCmd.ExecuteScalarAsync();
                    
                    return newId;
                }
                catch (Exception e)
                {   
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }
    }

    // public async Task ProcedureAsync()
    // {
    //     await using SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
    //     await using SqlCommand command = new SqlCommand();
    //     
    //     command.Connection = connection;
    //     await connection.OpenAsync();
    //     
    //     command.CommandText = "NazwaProcedury";
    //     command.CommandType = CommandType.StoredProcedure;
    //     
    //     command.Parameters.AddWithValue("@Id", 2);
    //     
    //     await command.ExecuteNonQueryAsync();
    //     
    // }
    
}