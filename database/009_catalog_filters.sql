alter table if exists mall_product add column if not exists brand varchar(100);
alter table if exists mall_product add column if not exists tags jsonb not null default '[]';
alter table if exists mall_product add column if not exists attributes jsonb not null default '{}';
alter table if exists mall_product add column if not exists sales_count integer not null default 0;
alter table if exists mall_product add column if not exists rating_average numeric(4,2) not null default 0;
alter table if exists mall_product add column if not exists review_count integer not null default 0;
alter table if exists mall_product add column if not exists is_recommended boolean not null default false;
create index if not exists ix_product_catalog_sort on mall_product(is_on_sale, is_recommended, sales_count, rating_average, min_price);
create index if not exists ix_product_brand on mall_product(brand);
