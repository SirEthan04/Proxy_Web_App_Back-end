CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) NOT NULL,
    `ProductVersion` varchar(32) NOT NULL,
    PRIMARY KEY (`MigrationId`)
);

START TRANSACTION;
CREATE TABLE `categories` (
    `id` varchar(36) NOT NULL,
    `name` varchar(100) NOT NULL,
    `description` varchar(255) NULL,
    `active` tinyint(1) NOT NULL,
    PRIMARY KEY (`id`),
    CONSTRAINT `ck_categories_name` CHECK (CHAR_LENGTH(TRIM(name)) > 0)
);

CREATE TABLE `roles` (
    `id` int NOT NULL,
    `name` varchar(30) NOT NULL,
    PRIMARY KEY (`id`),
    CONSTRAINT `ck_roles_name` CHECK (CHAR_LENGTH(TRIM(name)) > 0)
);

CREATE TABLE `products` (
    `id` varchar(36) NOT NULL,
    `category_id` varchar(36) NOT NULL,
    `name` varchar(150) NOT NULL,
    `sale_price` decimal(12,2) NOT NULL,
    `stock` int NOT NULL,
    `minimum_stock` int NOT NULL,
    `active` tinyint(1) NOT NULL,
    `version` int NOT NULL,
    PRIMARY KEY (`id`),
    CONSTRAINT `ck_products_name` CHECK (CHAR_LENGTH(TRIM(name)) > 0),
    CONSTRAINT `ck_products_price` CHECK (sale_price >= 0),
    CONSTRAINT `ck_products_stock` CHECK (stock >= 0 AND minimum_stock >= 0),
    CONSTRAINT `ck_products_version` CHECK (version >= 0),
    CONSTRAINT `FK_products_categories_category_id` FOREIGN KEY (`category_id`) REFERENCES `categories` (`id`) ON DELETE RESTRICT
);

CREATE TABLE `users` (
    `id` varchar(36) NOT NULL,
    `role_id` int NOT NULL,
    `name` varchar(120) NOT NULL,
    `email` varchar(180) NOT NULL,
    `password_hash` varchar(255) NOT NULL,
    `active` tinyint(1) NOT NULL,
    `created_at` datetime(6) NOT NULL,
    PRIMARY KEY (`id`),
    CONSTRAINT `ck_users_email` CHECK (CHAR_LENGTH(TRIM(email)) > 0),
    CONSTRAINT `ck_users_name` CHECK (CHAR_LENGTH(TRIM(name)) > 0),
    CONSTRAINT `ck_users_password` CHECK (CHAR_LENGTH(TRIM(password_hash)) > 0),
    CONSTRAINT `FK_users_roles_role_id` FOREIGN KEY (`role_id`) REFERENCES `roles` (`id`) ON DELETE RESTRICT
);

CREATE TABLE `product_lots` (
    `id` varchar(36) NOT NULL,
    `product_id` varchar(36) NOT NULL,
    `lot_code` varchar(80) NOT NULL,
    `expiration_date` date NULL,
    `quantity_available` int NOT NULL,
    `received_at` datetime(6) NOT NULL,
    PRIMARY KEY (`id`),
    CONSTRAINT `AK_product_lots_id_product_id` UNIQUE (`id`, `product_id`),
    CONSTRAINT `ck_product_lots_code` CHECK (CHAR_LENGTH(TRIM(lot_code)) > 0),
    CONSTRAINT `ck_product_lots_quantity` CHECK (quantity_available >= 0),
    CONSTRAINT `FK_product_lots_products_product_id` FOREIGN KEY (`product_id`) REFERENCES `products` (`id`) ON DELETE RESTRICT
);

CREATE TABLE `inventory_counts` (
    `id` varchar(36) NOT NULL,
    `user_id` varchar(36) NOT NULL,
    `counted_at` datetime(6) NOT NULL,
    `status` varchar(25) NOT NULL,
    `notes` varchar(500) NULL,
    PRIMARY KEY (`id`),
    CONSTRAINT `ck_inventory_counts_status` CHECK (status IN ('Draft', 'Confirmed', 'Cancelled')),
    CONSTRAINT `FK_inventory_counts_users_user_id` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE RESTRICT
);

CREATE TABLE `purchases` (
    `id` varchar(36) NOT NULL,
    `user_id` varchar(36) NOT NULL,
    `registered_at` datetime(6) NOT NULL,
    `status` varchar(25) NOT NULL,
    `total` decimal(12,2) NOT NULL,
    `created_at` datetime(6) NOT NULL,
    PRIMARY KEY (`id`),
    CONSTRAINT `ck_purchases_status` CHECK (status IN ('Draft', 'Confirmed', 'Cancelled')),
    CONSTRAINT `ck_purchases_total` CHECK (total >= 0),
    CONSTRAINT `FK_purchases_users_user_id` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE RESTRICT
);

CREATE TABLE `sales` (
    `id` varchar(36) NOT NULL,
    `user_id` varchar(36) NOT NULL,
    `registered_at` datetime(6) NOT NULL,
    `status` varchar(25) NOT NULL,
    `total` decimal(12,2) NOT NULL,
    `created_at` datetime(6) NOT NULL,
    PRIMARY KEY (`id`),
    CONSTRAINT `ck_sales_status` CHECK (status IN ('Draft', 'Confirmed', 'Cancelled')),
    CONSTRAINT `ck_sales_total` CHECK (total >= 0),
    CONSTRAINT `FK_sales_users_user_id` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE RESTRICT
);

CREATE TABLE `alerts` (
    `id` varchar(36) NOT NULL,
    `product_id` varchar(36) NOT NULL,
    `lot_id` varchar(36) NULL,
    `attended_by_user_id` varchar(36) NULL,
    `alert_type` varchar(30) NOT NULL,
    `status` varchar(25) NOT NULL,
    `message` varchar(255) NOT NULL,
    `created_at` datetime(6) NOT NULL,
    `attended_at` datetime(6) NULL,
    PRIMARY KEY (`id`),
    CONSTRAINT `ck_alerts_attendance` CHECK ((status = 'Pending' AND attended_at IS NULL AND attended_by_user_id IS NULL) OR (status = 'Attended' AND attended_at IS NOT NULL AND attended_by_user_id IS NOT NULL AND attended_at >= created_at)),
    CONSTRAINT `ck_alerts_expiration` CHECK (alert_type <> 'Expiration' OR lot_id IS NOT NULL),
    CONSTRAINT `ck_alerts_message` CHECK (CHAR_LENGTH(TRIM(message)) > 0),
    CONSTRAINT `ck_alerts_status` CHECK (status IN ('Pending', 'Attended')),
    CONSTRAINT `ck_alerts_type` CHECK (alert_type IN ('LowStock', 'OutOfStock', 'Expiration')),
    CONSTRAINT `FK_alerts_product_lots_lot_id_product_id` FOREIGN KEY (`lot_id`, `product_id`) REFERENCES `product_lots` (`id`, `product_id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_alerts_products_product_id` FOREIGN KEY (`product_id`) REFERENCES `products` (`id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_alerts_users_attended_by_user_id` FOREIGN KEY (`attended_by_user_id`) REFERENCES `users` (`id`) ON DELETE RESTRICT
);

CREATE TABLE `inventory_movements` (
    `id` varchar(36) NOT NULL,
    `product_id` varchar(36) NOT NULL,
    `lot_id` varchar(36) NULL,
    `user_id` varchar(36) NOT NULL,
    `movement_type` varchar(30) NOT NULL,
    `quantity_delta` int NOT NULL,
    `source_type` varchar(30) NOT NULL,
    `source_id` varchar(36) NOT NULL,
    `created_at` datetime(6) NOT NULL,
    PRIMARY KEY (`id`),
    CONSTRAINT `ck_inventory_movements_quantity` CHECK (quantity_delta <> 0),
    CONSTRAINT `ck_inventory_movements_sign` CHECK ((movement_type = 'Purchase' AND quantity_delta > 0) OR (movement_type IN ('Sale', 'Waste') AND quantity_delta < 0) OR movement_type = 'CountAdjustment'),
    CONSTRAINT `ck_inventory_movements_source` CHECK (CHAR_LENGTH(TRIM(source_type)) > 0 AND CHAR_LENGTH(source_id) = 36),
    CONSTRAINT `ck_inventory_movements_type` CHECK (movement_type IN ('Purchase', 'Sale', 'Waste', 'CountAdjustment')),
    CONSTRAINT `FK_inventory_movements_product_lots_lot_id_product_id` FOREIGN KEY (`lot_id`, `product_id`) REFERENCES `product_lots` (`id`, `product_id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_inventory_movements_products_product_id` FOREIGN KEY (`product_id`) REFERENCES `products` (`id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_inventory_movements_users_user_id` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE RESTRICT
);

CREATE TABLE `wastes` (
    `id` varchar(36) NOT NULL,
    `product_id` varchar(36) NOT NULL,
    `lot_id` varchar(36) NULL,
    `user_id` varchar(36) NOT NULL,
    `quantity` int NOT NULL,
    `reason` varchar(255) NOT NULL,
    `registered_at` datetime(6) NOT NULL,
    PRIMARY KEY (`id`),
    CONSTRAINT `ck_wastes_quantity` CHECK (quantity > 0),
    CONSTRAINT `ck_wastes_reason` CHECK (CHAR_LENGTH(TRIM(reason)) > 0),
    CONSTRAINT `FK_wastes_product_lots_lot_id_product_id` FOREIGN KEY (`lot_id`, `product_id`) REFERENCES `product_lots` (`id`, `product_id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_wastes_products_product_id` FOREIGN KEY (`product_id`) REFERENCES `products` (`id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_wastes_users_user_id` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE RESTRICT
);

CREATE TABLE `inventory_count_items` (
    `id` varchar(36) NOT NULL,
    `count_id` varchar(36) NOT NULL,
    `product_id` varchar(36) NOT NULL,
    `lot_id` varchar(36) NULL,
    `expected_quantity` int NOT NULL,
    `counted_quantity` int NOT NULL,
    `difference` int NOT NULL,
    PRIMARY KEY (`id`),
    CONSTRAINT `ck_inventory_count_items_quantities` CHECK (expected_quantity >= 0 AND counted_quantity >= 0 AND difference = counted_quantity - expected_quantity),
    CONSTRAINT `FK_inventory_count_items_inventory_counts_count_id` FOREIGN KEY (`count_id`) REFERENCES `inventory_counts` (`id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_inventory_count_items_product_lots_lot_id_product_id` FOREIGN KEY (`lot_id`, `product_id`) REFERENCES `product_lots` (`id`, `product_id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_inventory_count_items_products_product_id` FOREIGN KEY (`product_id`) REFERENCES `products` (`id`) ON DELETE RESTRICT
);

CREATE TABLE `purchase_items` (
    `id` varchar(36) NOT NULL,
    `purchase_id` varchar(36) NOT NULL,
    `product_id` varchar(36) NOT NULL,
    `lot_id` varchar(36) NULL,
    `quantity` int NOT NULL,
    `unit_cost` decimal(12,2) NOT NULL,
    `subtotal` decimal(12,2) NOT NULL,
    PRIMARY KEY (`id`),
    CONSTRAINT `ck_purchase_items_amounts` CHECK (quantity > 0 AND unit_cost >= 0 AND subtotal = quantity * unit_cost),
    CONSTRAINT `FK_purchase_items_product_lots_lot_id_product_id` FOREIGN KEY (`lot_id`, `product_id`) REFERENCES `product_lots` (`id`, `product_id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_purchase_items_products_product_id` FOREIGN KEY (`product_id`) REFERENCES `products` (`id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_purchase_items_purchases_purchase_id` FOREIGN KEY (`purchase_id`) REFERENCES `purchases` (`id`) ON DELETE RESTRICT
);

CREATE TABLE `sale_items` (
    `id` varchar(36) NOT NULL,
    `sale_id` varchar(36) NOT NULL,
    `product_id` varchar(36) NOT NULL,
    `lot_id` varchar(36) NULL,
    `quantity` int NOT NULL,
    `unit_price` decimal(12,2) NOT NULL,
    `subtotal` decimal(12,2) NOT NULL,
    PRIMARY KEY (`id`),
    CONSTRAINT `ck_sale_items_amounts` CHECK (quantity > 0 AND unit_price >= 0 AND subtotal = quantity * unit_price),
    CONSTRAINT `FK_sale_items_product_lots_lot_id_product_id` FOREIGN KEY (`lot_id`, `product_id`) REFERENCES `product_lots` (`id`, `product_id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_sale_items_products_product_id` FOREIGN KEY (`product_id`) REFERENCES `products` (`id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_sale_items_sales_sale_id` FOREIGN KEY (`sale_id`) REFERENCES `sales` (`id`) ON DELETE RESTRICT
);

CREATE INDEX `IX_alerts_attended_by_user_id` ON `alerts` (`attended_by_user_id`);

CREATE INDEX `IX_alerts_lot_id_product_id` ON `alerts` (`lot_id`, `product_id`);

CREATE INDEX `IX_alerts_product_id` ON `alerts` (`product_id`);

CREATE INDEX `IX_alerts_status_alert_type_created_at` ON `alerts` (`status`, `alert_type`, `created_at`);

CREATE UNIQUE INDEX `IX_categories_name` ON `categories` (`name`);

CREATE UNIQUE INDEX `IX_inventory_count_items_count_id_product_id_lot_id` ON `inventory_count_items` (`count_id`, `product_id`, `lot_id`);

CREATE INDEX `IX_inventory_count_items_lot_id_product_id` ON `inventory_count_items` (`lot_id`, `product_id`);

CREATE INDEX `IX_inventory_count_items_product_id` ON `inventory_count_items` (`product_id`);

CREATE INDEX `IX_inventory_counts_user_id` ON `inventory_counts` (`user_id`);

CREATE INDEX `IX_inventory_movements_lot_id_product_id` ON `inventory_movements` (`lot_id`, `product_id`);

CREATE INDEX `IX_inventory_movements_product_id_created_at` ON `inventory_movements` (`product_id`, `created_at`);

CREATE INDEX `IX_inventory_movements_source_type_source_id` ON `inventory_movements` (`source_type`, `source_id`);

CREATE INDEX `IX_inventory_movements_user_id` ON `inventory_movements` (`user_id`);

CREATE INDEX `IX_product_lots_expiration_date` ON `product_lots` (`expiration_date`);

CREATE UNIQUE INDEX `IX_product_lots_product_id_lot_code` ON `product_lots` (`product_id`, `lot_code`);

CREATE INDEX `IX_products_category_id` ON `products` (`category_id`);

CREATE INDEX `IX_purchase_items_lot_id_product_id` ON `purchase_items` (`lot_id`, `product_id`);

CREATE INDEX `IX_purchase_items_product_id` ON `purchase_items` (`product_id`);

CREATE INDEX `IX_purchase_items_purchase_id` ON `purchase_items` (`purchase_id`);

CREATE INDEX `IX_purchases_registered_at` ON `purchases` (`registered_at`);

CREATE INDEX `IX_purchases_user_id` ON `purchases` (`user_id`);

CREATE UNIQUE INDEX `IX_roles_name` ON `roles` (`name`);

CREATE INDEX `IX_sale_items_lot_id_product_id` ON `sale_items` (`lot_id`, `product_id`);

CREATE INDEX `IX_sale_items_product_id` ON `sale_items` (`product_id`);

CREATE INDEX `IX_sale_items_sale_id` ON `sale_items` (`sale_id`);

CREATE INDEX `IX_sales_registered_at` ON `sales` (`registered_at`);

CREATE INDEX `IX_sales_user_id` ON `sales` (`user_id`);

CREATE UNIQUE INDEX `IX_users_email` ON `users` (`email`);

CREATE INDEX `IX_users_role_id` ON `users` (`role_id`);

CREATE INDEX `IX_wastes_lot_id_product_id` ON `wastes` (`lot_id`, `product_id`);

CREATE INDEX `IX_wastes_product_id` ON `wastes` (`product_id`);

CREATE INDEX `IX_wastes_user_id` ON `wastes` (`user_id`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261108232740_CrearBaseDatosInicial', '10.0.12');

COMMIT;
