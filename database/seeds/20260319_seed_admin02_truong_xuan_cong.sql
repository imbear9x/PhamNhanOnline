begin;

with target_character as (
    select c.id as character_id
    from public.characters c
    join public.account_credentials ac on ac.account_id = c.account_id
    where ac.provider_user_id = 'admin02'
    order by c.created_at asc
    limit 1
),
upsert_player_martial_art as (
    insert into public.player_martial_arts (
        player_id,
        martial_art_id,
        current_stage,
        current_exp,
        created_at,
        updated_at
    )
    select
        tc.character_id,
        1,
        1,
        0,
        timezone('utc', now()),
        timezone('utc', now())
    from target_character tc
    on conflict (player_id, martial_art_id) do update
    set
        current_stage = greatest(public.player_martial_arts.current_stage, excluded.current_stage),
        current_exp = public.player_martial_arts.current_exp,
        updated_at = timezone('utc', now())
    returning id, player_id
),
upsert_player_skill as (
    insert into public.player_skills (
        player_id,
        skill_id,
        skill_group_code,
        source_type,
        source_martial_art_id,
        source_martial_art_skill_id,
        unlocked_at,
        is_active,
        created_at,
        updated_at
    )
    select
        tc.character_id,
        1,
        'moc_mien_chuong',
        2,
        1,
        1,
        timezone('utc', now()),
        true,
        timezone('utc', now()),
        timezone('utc', now())
    from target_character tc
    on conflict (player_id, skill_group_code) do update
    set
        skill_id = excluded.skill_id,
        skill_group_code = excluded.skill_group_code,
        source_type = excluded.source_type,
        source_martial_art_id = excluded.source_martial_art_id,
        source_martial_art_skill_id = excluded.source_martial_art_skill_id,
        is_active = true,
        updated_at = timezone('utc', now())
    returning id, player_id
)
insert into public.player_skill_loadouts (
    player_id,
    slot_index,
    player_skill_id,
    created_at,
    updated_at
)
select
    ups.player_id,
    1,
    ups.id,
    timezone('utc', now()),
    timezone('utc', now())
from upsert_player_skill ups
on conflict (player_id, slot_index) do update
set
    player_skill_id = excluded.player_skill_id,
    updated_at = timezone('utc', now());

update public.character_base_stats
set
    active_martial_art_id = 1
where character_id in (
    select character_id
    from (
        select c.id as character_id
        from public.characters c
        join public.account_credentials ac on ac.account_id = c.account_id
        where ac.provider_user_id = 'admin02'
        order by c.created_at asc
        limit 1
    ) target
);

commit;
