insert into sys_role (name, code) values ('平台管理员', 'admin') on conflict (code) do nothing;

insert into sys_permission (name, code) values
    ('用户管理', 'admin:users:read'),
    ('角色管理', 'admin:roles:write'),
    ('权限管理', 'admin:permissions:write'),
    ('商品管理', 'admin:products:write'),
    ('订单管理', 'admin:orders:write')
on conflict (code) do nothing;
