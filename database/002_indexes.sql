create index if not exists ix_user_address_user_default on user_address(user_id, is_default);
create index if not exists ix_mall_category_parent_sort on mall_category(parent_id, sort_order);
create index if not exists ix_mall_product_category_sale on mall_product(category_id, is_on_sale);
create index if not exists ix_mall_product_sku_product_enabled on mall_product_sku(product_id, enabled);
create index if not exists ix_mall_product_image_product_sort on mall_product_image(product_id, sort_order);
create index if not exists ix_mall_cart_user_checked on mall_cart(user_id, checked);
create index if not exists ix_mall_order_user_status_created on mall_order(user_id, status, created_at desc);
create index if not exists ix_mall_order_item_order on mall_order_item(order_id);
create index if not exists ix_payment_record_order_status on payment_record(order_id, status);
