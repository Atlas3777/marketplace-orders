using FluentMigrator;

namespace Marketplace.Orders.Migrations.Migrations;

[Migration(2026061101, "Create Orders Tables")]
public class CreateOrdersTablesMigration : Migration
{
    public override void Down()
    {
        Delete.Table("order_items");
        Delete.Table("orders");
    }

    public override void Up()
    {
        Create.Table("orders")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("userid").AsGuid().NotNullable()
            .WithColumn("totalprice").AsDecimal().NotNullable()
            .WithColumn("createdat").AsDateTime().NotNullable()
            .WithDefault(SystemMethods.CurrentUTCDateTime);

        Create.Table("order_items")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("orderid").AsGuid().NotNullable()
            .ForeignKey("orders", "id")
            .WithColumn("productid").AsGuid().NotNullable()
            .WithColumn("quantity").AsInt32().NotNullable()
            .WithColumn("price").AsDecimal().NotNullable();
    }
}