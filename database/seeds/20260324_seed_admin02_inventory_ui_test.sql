begin;

do $$
begin
    if not exists (
        select 1
        from public.characters c
        join public.account_credentials ac on ac.account_id = c.account_id
        where ac.provider_user_id = 'admin02'
    ) then
        raise exception 'Khong tim thay character nao cua account admin02.';
    end if;
end
$$;

with seeded_templates as (
    select *
    from (
        values
            (910001, 'kim_huyen_kiem', 'Kim Huyen Kiem', 1, 3, 1, true, true, true, 'item_kim_huyen_kiem', 'bg_item_rare', 'Thanh phi kiem trung pham, thich hop de test icon, rarity va item equipment trong inventory.'),
            (910002, 'thanh_van_phap_y', 'Thanh Van Phap Y', 1, 2, 1, true, true, true, 'item_thanh_van_phap_y', 'bg_item_uncommon', 'Phap y thanh sac cho tu si tan thu. Dung de test item ao trong inventory.'),
            (910003, 'thanh_phong_huyen_khau', 'Thanh Phong Huyen Khau', 1, 1, 1, true, true, true, 'item_thanh_phong_huyen_khau', 'bg_item_common', 'Huyen khau gian di, dung de test item quan trong inventory.'),
            (910004, 'van_hanh_bo', 'Van Hanh Bo', 1, 1, 1, true, true, true, 'item_van_hanh_bo', 'bg_item_common', 'Giay khinh than co ban, dung de test item giay trong inventory.'),
            (910005, 'hoi_linh_dan', 'Hoi Linh Dan', 2, 4, 99, true, true, true, 'item_hoi_linh_dan', 'bg_item_epic', 'Dan duoc phuc hoi linh luc, dung de test stackable consumable va tooltip description.'),
            (910006, 'truong_xuan_tam_phap_tan_ban', 'Truong Xuan Tam Phap - Tan Ban', 5, 3, 1, false, false, true, 'item_truong_xuan_tam_phap', 'bg_item_rare', 'Cong phap tan ban danh cho de tu moi nhap mon. Dung de test item loai Cong phap.'),
            (910007, 'ha_pham_linh_thach', 'Ha Pham Linh Thach', 6, 1, 9999, true, true, true, 'item_ha_pham_linh_thach', 'bg_item_common', 'Don vi tien te co ban trong gioi tu chan. Dung de test item stack lon.'),
            (910008, 'huyet_tinh_thao', 'Huyet Tinh Thao', 3, 2, 999, true, true, true, 'item_huyet_tinh_thao', 'bg_item_uncommon', 'Nguyen lieu duoc tinh co the dung cho luyen dan hoac che tao.'),
            (910009, 'thanh_moc_phu', 'Thanh Moc Phu', 4, 4, 1, true, true, true, 'item_thanh_moc_phu', 'bg_item_epic', 'Phap bao dang phu luu chuyen moc linh khi. Dung de test item loai Phap bao.')
    ) as v(id, code, name, item_type, rarity, max_stack, is_tradeable, is_droppable, is_destroyable, icon, background_icon, description)
),
upsert_item_templates as (
    insert into public.item_templates (
        id,
        code,
        name,
        item_type,
        rarity,
        max_stack,
        is_tradeable,
        is_droppable,
        is_destroyable,
        icon,
        background_icon,
        description,
        created_at
    )
    select
        st.id,
        st.code,
        st.name,
        st.item_type,
        st.rarity,
        st.max_stack,
        st.is_tradeable,
        st.is_droppable,
        st.is_destroyable,
        st.icon,
        st.background_icon,
        st.description,
        timezone('utc', now())
    from seeded_templates st
    on conflict (id) do update
    set
        code = excluded.code,
        name = excluded.name,
        item_type = excluded.item_type,
        rarity = excluded.rarity,
        max_stack = excluded.max_stack,
        is_tradeable = excluded.is_tradeable,
        is_droppable = excluded.is_droppable,
        is_destroyable = excluded.is_destroyable,
        icon = excluded.icon,
        background_icon = excluded.background_icon,
        description = excluded.description
    returning id
),
upsert_equipment_templates as (
    insert into public.equipment_templates (
        item_template_id,
        slot_type,
        equipment_type,
        level_requirement
    )
    values
        (910001, 1, 1, 12),
        (910002, 2, 3, 10),
        (910003, 3, 4, 8),
        (910004, 4, 5, 8)
    on conflict (item_template_id) do update
    set
        slot_type = excluded.slot_type,
        equipment_type = excluded.equipment_type,
        level_requirement = excluded.level_requirement
    returning item_template_id
),
target_character as (
    select c.id as character_id
    from public.characters c
    join public.account_credentials ac on ac.account_id = c.account_id
    where ac.provider_user_id = 'admin02'
    order by c.created_at asc
    limit 1
),
delete_old_equipment_rows as (
    delete from public.player_equipments pe
    where pe.player_item_id in (
        select pi.id
        from public.player_items pi
        join target_character tc on tc.character_id = pi.player_id
        where pi.item_template_id in (select id from seeded_templates)
    )
    returning player_item_id
),
delete_old_items as (
    delete from public.player_items pi
    where pi.player_id in (select character_id from target_character)
      and pi.item_template_id in (select id from seeded_templates)
    returning id
),
seeded_inventory_rows as (
    select *
    from (
        values
            (910001, 1, false),
            (910002, 1, false),
            (910003, 1, false),
            (910004, 1, false),
            (910005, 12, false),
            (910006, 1, true),
            (910007, 128, false),
            (910008, 36, false),
            (910009, 1, false)
    ) as v(item_template_id, quantity, is_bound)
),
inserted_player_items as (
    insert into public.player_items (
        player_id,
        item_template_id,
        quantity,
        is_bound,
        acquired_at,
        expire_at,
        updated_at
    )
    select
        tc.character_id,
        sir.item_template_id,
        sir.quantity,
        sir.is_bound,
        timezone('utc', now()),
        null,
        timezone('utc', now())
    from target_character tc
    cross join seeded_inventory_rows sir
    returning id, item_template_id
)
insert into public.player_equipments (
    player_item_id,
    equipped_slot,
    enhance_level,
    durability,
    updated_at
)
select
    ipi.id,
    null,
    details.enhance_level,
    details.durability,
    timezone('utc', now())
from inserted_player_items ipi
join (
    values
        (910001, 3, 82),
        (910002, 1, 55),
        (910003, 0, 41),
        (910004, 2, 63)
) as details(item_template_id, enhance_level, durability) on details.item_template_id = ipi.item_template_id
on conflict (player_item_id) do update
set
    equipped_slot = excluded.equipped_slot,
    enhance_level = excluded.enhance_level,
    durability = excluded.durability,
    updated_at = excluded.updated_at;

commit;
