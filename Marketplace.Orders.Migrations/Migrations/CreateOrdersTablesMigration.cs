using FluentMigrator;

namespace Marketplace.Orders.Migrations.Migrations;

[Migration(202606220033, "Create Orders Tables")]
public class CreateOrdersTablesMigration : Migration
{
    public override void Down()
    {
        if (Schema.Table("order_items").Exists())
            Delete.Table("order_items");
        if (Schema.Table("orders").Exists())
            Delete.Table("orders");
    }

    public override void Up()
    {
        if (!Schema.Table("orders").Exists())
        {
            Create.Table("orders")
                .WithColumn("id").AsGuid().PrimaryKey()
                .WithColumn("userid").AsGuid().NotNullable()
                .WithColumn("totalprice").AsDecimal().NotNullable()
                .WithColumn("status").AsInt32().NotNullable().WithDefaultValue(1) 
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithDefault(SystemMethods.CurrentUTCDateTime);
        }

        if (!Schema.Table("order_items").Exists())
        {
            Create.Table("order_items")
                .WithColumn("id").AsGuid().PrimaryKey()
                .WithColumn("orderid").AsGuid().NotNullable()
                .ForeignKey("orders", "id")
                .WithColumn("productid").AsGuid().NotNullable()
                .WithColumn("quantity").AsInt32().NotNullable()
                .WithColumn("price").AsDecimal().NotNullable();
        }
    }
}