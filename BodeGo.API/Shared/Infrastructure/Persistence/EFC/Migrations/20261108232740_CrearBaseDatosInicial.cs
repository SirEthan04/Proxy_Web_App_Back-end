using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BodeGo.API.Shared.Infrastructure.Persistence.EFC.Migrations
{
    /// <inheritdoc />
    public partial class CrearBaseDatosInicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false),
                    name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    active = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categories", x => x.id);
                    table.CheckConstraint("ck_categories_name", "CHAR_LENGTH(TRIM(name)) > 0");
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false),
                    name = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.id);
                    table.CheckConstraint("ck_roles_name", "CHAR_LENGTH(TRIM(name)) > 0");
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false),
                    category_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false),
                    name = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    sale_price = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    stock = table.Column<int>(type: "int", nullable: false),
                    minimum_stock = table.Column<int>(type: "int", nullable: false),
                    active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    version = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_products", x => x.id);
                    table.CheckConstraint("ck_products_name", "CHAR_LENGTH(TRIM(name)) > 0");
                    table.CheckConstraint("ck_products_price", "sale_price >= 0");
                    table.CheckConstraint("ck_products_stock", "stock >= 0 AND minimum_stock >= 0");
                    table.CheckConstraint("ck_products_version", "version >= 0");
                    table.ForeignKey(
                        name: "FK_products_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false),
                    role_id = table.Column<int>(type: "int", nullable: false),
                    name = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                    email = table.Column<string>(type: "varchar(180)", maxLength: 180, nullable: false),
                    password_hash = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                    table.CheckConstraint("ck_users_email", "CHAR_LENGTH(TRIM(email)) > 0");
                    table.CheckConstraint("ck_users_name", "CHAR_LENGTH(TRIM(name)) > 0");
                    table.CheckConstraint("ck_users_password", "CHAR_LENGTH(TRIM(password_hash)) > 0");
                    table.ForeignKey(
                        name: "FK_users_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "product_lots",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false),
                    product_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false),
                    lot_code = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    expiration_date = table.Column<DateOnly>(type: "date", nullable: true),
                    quantity_available = table.Column<int>(type: "int", nullable: false),
                    received_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_lots", x => x.id);
                    table.UniqueConstraint("AK_product_lots_id_product_id", x => new { x.id, x.product_id });
                    table.CheckConstraint("ck_product_lots_code", "CHAR_LENGTH(TRIM(lot_code)) > 0");
                    table.CheckConstraint("ck_product_lots_quantity", "quantity_available >= 0");
                    table.ForeignKey(
                        name: "FK_product_lots_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "inventory_counts",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false),
                    user_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false),
                    counted_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    status = table.Column<string>(type: "varchar(25)", maxLength: 25, nullable: false),
                    notes = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_counts", x => x.id);
                    table.CheckConstraint("ck_inventory_counts_status", "status IN ('Draft', 'Confirmed', 'Cancelled')");
                    table.ForeignKey(
                        name: "FK_inventory_counts_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "purchases",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false),
                    user_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false),
                    registered_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    status = table.Column<string>(type: "varchar(25)", maxLength: 25, nullable: false),
                    total = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchases", x => x.id);
                    table.CheckConstraint("ck_purchases_status", "status IN ('Draft', 'Confirmed', 'Cancelled')");
                    table.CheckConstraint("ck_purchases_total", "total >= 0");
                    table.ForeignKey(
                        name: "FK_purchases_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sales",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false),
                    user_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false),
                    registered_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    status = table.Column<string>(type: "varchar(25)", maxLength: 25, nullable: false),
                    total = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sales", x => x.id);
                    table.CheckConstraint("ck_sales_status", "status IN ('Draft', 'Confirmed', 'Cancelled')");
                    table.CheckConstraint("ck_sales_total", "total >= 0");
                    table.ForeignKey(
                        name: "FK_sales_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "alerts",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false),
                    product_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false),
                    lot_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true),
                    attended_by_user_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true),
                    alert_type = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    status = table.Column<string>(type: "varchar(25)", maxLength: 25, nullable: false),
                    message = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    attended_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alerts", x => x.id);
                    table.CheckConstraint("ck_alerts_attendance", "(status = 'Pending' AND attended_at IS NULL AND attended_by_user_id IS NULL) OR (status = 'Attended' AND attended_at IS NOT NULL AND attended_by_user_id IS NOT NULL AND attended_at >= created_at)");
                    table.CheckConstraint("ck_alerts_expiration", "alert_type <> 'Expiration' OR lot_id IS NOT NULL");
                    table.CheckConstraint("ck_alerts_message", "CHAR_LENGTH(TRIM(message)) > 0");
                    table.CheckConstraint("ck_alerts_status", "status IN ('Pending', 'Attended')");
                    table.CheckConstraint("ck_alerts_type", "alert_type IN ('LowStock', 'OutOfStock', 'Expiration')");
                    table.ForeignKey(
                        name: "FK_alerts_product_lots_lot_id_product_id",
                        columns: x => new { x.lot_id, x.product_id },
                        principalTable: "product_lots",
                        principalColumns: new[] { "id", "product_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_alerts_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_alerts_users_attended_by_user_id",
                        column: x => x.attended_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "inventory_movements",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false),
                    product_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false),
                    lot_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true),
                    user_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false),
                    movement_type = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    quantity_delta = table.Column<int>(type: "int", nullable: false),
                    source_type = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    source_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_movements", x => x.id);
                    table.CheckConstraint("ck_inventory_movements_quantity", "quantity_delta <> 0");
                    table.CheckConstraint("ck_inventory_movements_sign", "(movement_type = 'Purchase' AND quantity_delta > 0) OR (movement_type IN ('Sale', 'Waste') AND quantity_delta < 0) OR movement_type = 'CountAdjustment'");
                    table.CheckConstraint("ck_inventory_movements_source", "CHAR_LENGTH(TRIM(source_type)) > 0 AND CHAR_LENGTH(source_id) = 36");
                    table.CheckConstraint("ck_inventory_movements_type", "movement_type IN ('Purchase', 'Sale', 'Waste', 'CountAdjustment')");
                    table.ForeignKey(
                        name: "FK_inventory_movements_product_lots_lot_id_product_id",
                        columns: x => new { x.lot_id, x.product_id },
                        principalTable: "product_lots",
                        principalColumns: new[] { "id", "product_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_movements_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_movements_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "wastes",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false),
                    product_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false),
                    lot_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true),
                    user_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false),
                    quantity = table.Column<int>(type: "int", nullable: false),
                    reason = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    registered_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wastes", x => x.id);
                    table.CheckConstraint("ck_wastes_quantity", "quantity > 0");
                    table.CheckConstraint("ck_wastes_reason", "CHAR_LENGTH(TRIM(reason)) > 0");
                    table.ForeignKey(
                        name: "FK_wastes_product_lots_lot_id_product_id",
                        columns: x => new { x.lot_id, x.product_id },
                        principalTable: "product_lots",
                        principalColumns: new[] { "id", "product_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_wastes_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_wastes_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "inventory_count_items",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false),
                    count_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false),
                    product_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false),
                    lot_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true),
                    expected_quantity = table.Column<int>(type: "int", nullable: false),
                    counted_quantity = table.Column<int>(type: "int", nullable: false),
                    difference = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_count_items", x => x.id);
                    table.CheckConstraint("ck_inventory_count_items_quantities", "expected_quantity >= 0 AND counted_quantity >= 0 AND difference = counted_quantity - expected_quantity");
                    table.ForeignKey(
                        name: "FK_inventory_count_items_inventory_counts_count_id",
                        column: x => x.count_id,
                        principalTable: "inventory_counts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_count_items_product_lots_lot_id_product_id",
                        columns: x => new { x.lot_id, x.product_id },
                        principalTable: "product_lots",
                        principalColumns: new[] { "id", "product_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_count_items_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "purchase_items",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false),
                    purchase_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false),
                    product_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false),
                    lot_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true),
                    quantity = table.Column<int>(type: "int", nullable: false),
                    unit_cost = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    subtotal = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_items", x => x.id);
                    table.CheckConstraint("ck_purchase_items_amounts", "quantity > 0 AND unit_cost >= 0 AND subtotal = quantity * unit_cost");
                    table.ForeignKey(
                        name: "FK_purchase_items_product_lots_lot_id_product_id",
                        columns: x => new { x.lot_id, x.product_id },
                        principalTable: "product_lots",
                        principalColumns: new[] { "id", "product_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_items_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_items_purchases_purchase_id",
                        column: x => x.purchase_id,
                        principalTable: "purchases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sale_items",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false),
                    sale_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false),
                    product_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false),
                    lot_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true),
                    quantity = table.Column<int>(type: "int", nullable: false),
                    unit_price = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    subtotal = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sale_items", x => x.id);
                    table.CheckConstraint("ck_sale_items_amounts", "quantity > 0 AND unit_price >= 0 AND subtotal = quantity * unit_price");
                    table.ForeignKey(
                        name: "FK_sale_items_product_lots_lot_id_product_id",
                        columns: x => new { x.lot_id, x.product_id },
                        principalTable: "product_lots",
                        principalColumns: new[] { "id", "product_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sale_items_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sale_items_sales_sale_id",
                        column: x => x.sale_id,
                        principalTable: "sales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_alerts_attended_by_user_id",
                table: "alerts",
                column: "attended_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_alerts_lot_id_product_id",
                table: "alerts",
                columns: new[] { "lot_id", "product_id" });

            migrationBuilder.CreateIndex(
                name: "IX_alerts_product_id",
                table: "alerts",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_alerts_status_alert_type_created_at",
                table: "alerts",
                columns: new[] { "status", "alert_type", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_categories_name",
                table: "categories",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_count_items_count_id_product_id_lot_id",
                table: "inventory_count_items",
                columns: new[] { "count_id", "product_id", "lot_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_count_items_lot_id_product_id",
                table: "inventory_count_items",
                columns: new[] { "lot_id", "product_id" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_count_items_product_id",
                table: "inventory_count_items",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_counts_user_id",
                table: "inventory_counts",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_movements_lot_id_product_id",
                table: "inventory_movements",
                columns: new[] { "lot_id", "product_id" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_movements_product_id_created_at",
                table: "inventory_movements",
                columns: new[] { "product_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_movements_source_type_source_id",
                table: "inventory_movements",
                columns: new[] { "source_type", "source_id" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_movements_user_id",
                table: "inventory_movements",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_lots_expiration_date",
                table: "product_lots",
                column: "expiration_date");

            migrationBuilder.CreateIndex(
                name: "IX_product_lots_product_id_lot_code",
                table: "product_lots",
                columns: new[] { "product_id", "lot_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_products_category_id",
                table: "products",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_items_lot_id_product_id",
                table: "purchase_items",
                columns: new[] { "lot_id", "product_id" });

            migrationBuilder.CreateIndex(
                name: "IX_purchase_items_product_id",
                table: "purchase_items",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_items_purchase_id",
                table: "purchase_items",
                column: "purchase_id");

            migrationBuilder.CreateIndex(
                name: "IX_purchases_registered_at",
                table: "purchases",
                column: "registered_at");

            migrationBuilder.CreateIndex(
                name: "IX_purchases_user_id",
                table: "purchases",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_roles_name",
                table: "roles",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sale_items_lot_id_product_id",
                table: "sale_items",
                columns: new[] { "lot_id", "product_id" });

            migrationBuilder.CreateIndex(
                name: "IX_sale_items_product_id",
                table: "sale_items",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_sale_items_sale_id",
                table: "sale_items",
                column: "sale_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_registered_at",
                table: "sales",
                column: "registered_at");

            migrationBuilder.CreateIndex(
                name: "IX_sales_user_id",
                table: "sales",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_role_id",
                table: "users",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_wastes_lot_id_product_id",
                table: "wastes",
                columns: new[] { "lot_id", "product_id" });

            migrationBuilder.CreateIndex(
                name: "IX_wastes_product_id",
                table: "wastes",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_wastes_user_id",
                table: "wastes",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "alerts");

            migrationBuilder.DropTable(
                name: "inventory_count_items");

            migrationBuilder.DropTable(
                name: "inventory_movements");

            migrationBuilder.DropTable(
                name: "purchase_items");

            migrationBuilder.DropTable(
                name: "sale_items");

            migrationBuilder.DropTable(
                name: "wastes");

            migrationBuilder.DropTable(
                name: "inventory_counts");

            migrationBuilder.DropTable(
                name: "purchases");

            migrationBuilder.DropTable(
                name: "sales");

            migrationBuilder.DropTable(
                name: "product_lots");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "products");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "categories");
        }
    }
}
