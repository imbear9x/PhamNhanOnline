namespace AdminDesignerTool;

internal sealed record AdminResourceDefinition(
    string Key,
    string Category,
    string DisplayName,
    string TableName,
    string? DefaultOrderBy,
    string Description,
    string HelpText,
    AdminEditorKind EditorKind = AdminEditorKind.GenericTable)
{
    public string SelectSql =>
        string.IsNullOrWhiteSpace(DefaultOrderBy)
            ? $"select * from public.{TableName};"
            : $"select * from public.{TableName} order by {DefaultOrderBy};";
}

internal static class AdminResourceCatalog
{
    public static IReadOnlyList<AdminResourceDefinition> Build()
    {
        return
        [
            new("martial_art_workspace", "Designer Workspace", "Martial Art Workspace", string.Empty, null, "Workspace chuyen biet de chinh cong phap, tang va skill unlock theo kieu master-detail.", "Chon cong phap o bang tren, cac tab ben duoi tu dong loc theo cong phap dang chon. Cac bang sau nhu stage bonus va scaling van co the edit o grid generic ben duoi nhom Cong Phap.", AdminEditorKind.MartialArtWorkspace),
            new("craft_recipe_workspace", "Designer Workspace", "Craft Recipe Workspace", string.Empty, null, "Workspace chuyen biet de chinh recipe, requirement va mutation bonus theo cung 1 man hinh.", "Chon recipe o bang tren, cac tab ben duoi tu dong loc theo recipe dang chon. Khi them dong moi, tool se tu dien craft_recipe_id.", AdminEditorKind.CraftRecipeWorkspace),
            new("equipment_workspace", "Designer Workspace", "Equipment Workspace", string.Empty, null, "Workspace chuyen biet de chinh equipment item template, equipment core va base stat.", "Chi hien thi item template co item_type = Equipment. Chon 1 item template o bang tren de edit phan equipment core va base stat ben duoi.", AdminEditorKind.EquipmentWorkspace),
            new("map_workspace", "Designer Workspace", "Map Workspace", string.Empty, null, "Workspace de tao map va cac zone slot ngay trong cung 1 man hinh.", "Tao map o bang tren, chon map dang sua, zone slot ben duoi se loc theo map do va tu dien map_template_id.", AdminEditorKind.MapWorkspace),
            new("pill_recipe_workspace", "Designer Workspace", "Pill Recipe Workspace", string.Empty, null, "Workspace de chinh dan phuong, input va moc mastery theo kieu master-detail.", "Tao dan phuong o bang tren, chon 1 dong de sua nguyen lieu va cac moc thong thao ben duoi.", AdminEditorKind.PillRecipeWorkspace),
            new("pill_workspace", "Designer Workspace", "Pill Workspace", string.Empty, null, "Workspace de chinh pill template va cac effect cua vien dan.", "Tao pill template o bang tren, chon 1 dong de sua cac effect ben duoi.", AdminEditorKind.PillWorkspace),
            new("herb_workspace", "Designer Workspace", "Herb Workspace", string.Empty, null, "Workspace de chinh herb template, moc tang truong va output thu hoach.", "Tao herb o bang tren, chon 1 dong de sua growth stage va harvest output ben duoi.", AdminEditorKind.HerbWorkspace),
            new("game_random_workspace", "Designer Workspace", "Game Random Workspace", string.Empty, null, "Workspace de chinh random table, entries va fortune tags theo bang random cha.", "Tao random table o bang tren, chon 1 dong de sua entries va fortune tags ben duoi.", AdminEditorKind.GameRandomWorkspace),
            new("enemy_workspace", "Designer Workspace", "Enemy Workspace", string.Empty, null, "Workspace de chinh enemy template, skill va reward rule tren cung mot man hinh.", "Tao enemy o bang tren, chon 1 dong de sua skill va reward rule ben duoi.", AdminEditorKind.EnemyWorkspace),
            new("enemy_spawn_workspace", "Designer Workspace", "Enemy Spawn Workspace", string.Empty, null, "Workspace de chinh spawn group va cac spawn entry theo tung map.", "Tao spawn group o bang tren, chon 1 dong de sua danh sach enemy spawn ben duoi.", AdminEditorKind.EnemySpawnWorkspace),
            new("player_inventory_workspace", "Designer Workspace", "Player Inventory Workspace", string.Empty, null, "Workspace để seed item trực tiếp cho từng nhân vật, gồm item instance, equipment instance và bonus stat riêng.", "Chọn nhân vật ở bảng trên, sau đó thêm item ở tab Player Items. Nếu item là equipment và cần dữ liệu instance hoặc bonus stat riêng thì cấu hình tiếp ở các tab bên dưới.", AdminEditorKind.PlayerInventoryWorkspace),

            new("martial_arts", "Cong Phap", "Cong Phap", "martial_arts", "id", "Dinh nghia cong phap goc. Nen tao cong phap truoc roi moi them stage/skill unlock.", "Thu tu khuyen nghi: tao cong phap -> tao cac tang -> them bonus tang -> map skill unlock.\r\nLuu y: bang nay dung id thu cong, nen game design can co quy uoc dat id/code de tranh trung."),
            new("martial_art_stages", "Cong Phap", "Tang Cong Phap", "martial_art_stages", "martial_art_id, stage_level", "Moi tang cua cong phap, gom exp, bottleneck va rate dot pha.", "Moi dong phai tro den mot martial_art_id hop le.\r\nNen giu stage_level tang dan tu 1..max_stage cua cong phap."),
            new("martial_art_stage_stat_bonuses", "Cong Phap", "Bonus Tang Cong Phap", "martial_art_stage_stat_bonuses", "martial_art_stage_id, id", "Bonus stat theo tung tang cong phap.", "Bang nay dung de buff stat theo tang.\r\nstat_type va value_type hien la enum dang luu duoi dang so, can thong nhat bang enum voi team code."),
            new("skills", "Cong Phap", "Skill", "skills", "id", "Skill goc. Nen tao skill truoc roi moi them effect va scaling.", "Thu tu khuyen nghi: tao skill -> them effect -> map vao cong phap -> them scaling.\r\nskill_type va target_type la enum dang luu so."),
            new("skill_effects", "Cong Phap", "Skill Effects", "skill_effects", "skill_id, order_index", "Danh sach effect cua skill.", "Moi skill co nhieu effect theo order_index.\r\nChi can dien cac cot phu hop voi effect_type va formula_type dang dung."),
            new("martial_art_skills", "Cong Phap", "Unlock Skill Tu Cong Phap", "martial_art_skills", "martial_art_id, unlock_stage, id", "Map cong phap -> skill duoc mo khoa o stage nao.", "Can tao truoc martial_art va skill.\r\nunlock_stage nen nam trong range tang cua cong phap."),
            new("martial_art_skill_scalings", "Cong Phap", "Scaling Skill Theo Cong Phap", "martial_art_skill_scalings", "martial_art_skill_id, id", "Scaling cho toan skill hoac cho effect cu the.", "Neu skill_effect_id rong thi scaling ap cho toan skill.\r\nscaling_target va value_type la enum dang luu so."),

            new("item_templates", "Item & Equipment", "Item Templates", "item_templates", "id", "Container tong cho moi item trong game.", "Bang trung tam cua item system.\r\nNen tao item template truoc, sau do moi them equipment template hoac martial art book."),
            new("equipment_templates", "Item & Equipment", "Equipment Templates", "equipment_templates", "item_template_id", "Thong tin equipment cho item_type = Equipment.", "item_template_id phai tro den mot item template co loai equipment.\r\nequipment_slot va quality la enum dang luu so."),
            new("equipment_template_stats", "Item & Equipment", "Equipment Template Stats", "equipment_template_stats", "equipment_template_id, id", "Stat goc cua trang bi.", "Mot trang bi co the co nhieu dong stat.\r\nstat_type va value_type la enum dang luu so."),
            new("martial_art_book_templates", "Item & Equipment", "Martial Art Books", "martial_art_book_templates", "item_template_id", "Map item sach cong phap -> martial_art.", "Dung cho item sach hoc cong phap.\r\nCan co san item_template va martial_art truoc khi tao mapping."),

            new("craft_recipes", "Che Tao", "Craft Recipes", "craft_recipes", "id", "Cong thuc craft goc, output, success rate, mutation rate.", "Thu tu khuyen nghi: tao output item -> tao recipe -> them requirements -> them mutation bonus neu can.\r\ncost_currency_value hien chua duoc runtime ho tro."),
            new("craft_recipe_requirements", "Che Tao", "Craft Requirements", "craft_recipe_requirements", "craft_recipe_id, id", "Nguyen lieu/requirement cua recipe, gom ca optional requirement.", "Moi requirement tro den mot item template.\r\nNeu la non-stackable item, runtime se can player_item_id cu the luc craft."),
            new("craft_recipe_mutation_bonuses", "Che Tao", "Craft Mutation Bonuses", "craft_recipe_mutation_bonuses", "craft_recipe_id, id", "Bonus instance sinh ra neu craft mutation thanh cong.", "Bang nay hien co y nghia ro nhat voi output la equipment.\r\nstat_type va value_type la enum dang luu so."),

            new("pill_templates", "Dan Duoc & Duoc Vien", "Pill Templates", "pill_templates", "item_template_id", "Template dan duoc gan voi item consumable/passive material.", "Thu tu khuyen nghi: tao item pill truoc -> tao pill template -> them cac effect.\r\nPhase nay chi config template va recipe; runtime consume pill se lam o phase sau."),
            new("pill_effects", "Dan Duoc & Duoc Vien", "Pill Effects", "pill_effects", "pill_template_id, order_index", "Danh sach effect cua tung pill.", "Moi pill co the co nhieu effect theo order_index.\r\nvalue_type, stat_type dang luu enum duoi dang so."),
            new("pill_recipe_templates", "Dan Duoc & Duoc Vien", "Pill Recipe Templates", "pill_recipe_templates", "id", "Dan phuong tach rieng khoi craft recipe thuong.", "Moi recipe tro den item sach dan phuong va item pill ket qua.\r\nThanh cong that bai va mastery duoc xu ly boi AlchemyService rieng."),
            new("pill_recipe_inputs", "Dan Duoc & Duoc Vien", "Pill Recipe Inputs", "pill_recipe_inputs", "pill_recipe_template_id, id", "Nguyen lieu va optional input cua dan phuong.", "required_herb_maturity da co schema, nhung runtime phase nay chua cho craft truc tiep tu cay dang trong.\r\nNen uu tien dung item duoc lieu thu hoach ra inventory."),
            new("pill_recipe_mastery_stages", "Dan Duoc & Duoc Vien", "Pill Recipe Mastery", "pill_recipe_mastery_stages", "pill_recipe_template_id, required_total_craft_count", "Moc thong thao cua tung dan phuong.", "Moi moc se tang them success rate bonus.\r\nRuntime lay moc cao nhat da dat duoc, khong cong don tat ca cac moc."),
            new("soil_templates", "Dan Duoc & Duoc Vien", "Soil Templates", "soil_templates", "item_template_id", "Linh tho/dat trong dung cho duoc vien.", "item_template_id phai tro den item type = Soil.\r\nSoil la item non-stackable, co toc do tang truong va thoi gian hieu luc toi da."),
            new("herb_templates", "Dan Duoc & Duoc Vien", "Herb Templates", "herb_templates", "id", "Template duoc lieu trong duoc vien.", "Moi herb nen tro den 2 item template: 1 seed item de trong moi, 1 herb plant item de nho cay non/cay song vao tui roi trong lai.\r\nCac moc tang truong va output thu hoach duoc config o bang con."),
            new("herb_growth_stage_configs", "Dan Duoc & Duoc Vien", "Herb Growth Stages", "herb_growth_stage_configs", "herb_template_id, required_growth_seconds", "Moc tang truong va tuoi duoc lieu.", "Nen config stage tang dan tu Seedling -> Mature -> Perfect.\r\nrequired_growth_seconds la tong so giay tang truong tich luy de dat moc do."),
            new("herb_harvest_outputs", "Dan Duoc & Duoc Vien", "Herb Harvest Outputs", "herb_harvest_outputs", "herb_template_id, required_stage, id", "Vat pham nhan duoc khi thu hoach duoc lieu.", "Runtime uu tien output dung stage hien tai; neu khong co se lay moc stage thap hon gan nhat.\r\nCo the tra ra duoc lieu, hat giong, hoac item khac tuy design."),

            new("realm_templates", "Balance", "Realm Templates", "realm_templates", "id", "Canh gioi tu luyen.", "Bang balance canh gioi hien dang duoc server su dung.\r\nSua cac he so o day se anh huong progression."),
            new("potential_stat_upgrade_tiers", "Balance", "Potential Upgrade Tiers", "potential_stat_upgrade_tiers", "target_stat, tier_index", "Config tier nang stat bang potential.", "target_stat map voi enum PotentialAllocationTarget trong GameShared.\r\nHien dang co None, BaseHp, BaseMp, BaseAttack, BaseSpeed, BaseSpiritualSense, BaseFortune."),
            new("spiritual_energy_templates", "Balance", "Spiritual Energy Templates", "spiritual_energy_templates", "id", "Template linh khi cho zone/map.", "Template linh khi de map/zone tro vao.\r\nNen sua thong so voi mindset balance toan map."),

            new("game_random_tables", "Random & Drop", "Game Random Tables", "game_random_tables", "id", "Bang config random/drop tong quat cua game.", "Moi bang random co `table_id` dung trong code/runtime. Fortune modifier va none entry duoc config truc tiep o day."),
            new("game_random_entries", "Random & Drop", "Game Random Entries", "game_random_entries", "game_random_table_id, order_index", "Danh sach entry cua tung bang random.", "Moi entry thuoc ve 1 bang random. `order_index` giup giu thu tu cau hinh, `chance_parts_per_million` dung scale 1_000_000."),
            new("game_random_entry_tags", "Random & Drop", "Game Random Entry Tags", "game_random_entry_tags", "game_random_entry_id, id", "Tag cua tung entry de fortune modifier co the ap dung co dieu kien.", "Neu bang random chi can fortune ap cho mot so loai entry thi config tag o day."),
            new("game_random_fortune_tags", "Random & Drop", "Game Random Fortune Tags", "game_random_fortune_tags", "game_random_table_id, id", "Danh sach tag duoc phep nhan fortune bonus trong bang random.", "Neu de trong, fortune se ap cho tat ca entry hop le. Neu co du lieu, chi entry nao mang mot trong cac tag nay moi duoc tang chance."),

            new("enemy_templates", "Enemy & Boss", "Enemy Templates", "enemy_templates", "id", "Dinh nghia enemy/boss goc cho map runtime.", "Tao template quái hoặc boss ở đây. Sau đó thêm skill và reward rule để hoàn thiện hành vi, rồi map vào spawn group."),
            new("enemy_template_skills", "Enemy & Boss", "Enemy Skills", "enemy_template_skills", "enemy_template_id, order_index", "Danh sach skill ma enemy co the su dung.", "Mỗi enemy có thể có nhiều skill. Runtime phase đầu không dùng MP, chỉ check cooldown và minimum_skill_interval_ms."),
            new("enemy_reward_rules", "Enemy & Boss", "Enemy Reward Rules", "enemy_reward_rules", "enemy_template_id, order_index", "Rule phat thuong khi enemy/boss chet.", "Mỗi rule chọn 1 kiểu phát thưởng: rơi xuống đất hoặc phát thẳng. target_rule quyết định ai được nhận: đủ điều kiện, last hit hoặc top damage."),

            new("map_enemy_spawn_groups", "Enemy & Boss", "Enemy Spawn Groups", "map_enemy_spawn_groups", "map_template_id, id", "Nhom spawn quái/boss theo map runtime.", "Một group quyết định map nào có spawn gì, phạm vi runtime nào, có phải boss spawn không, respawn theo timer thế nào."),
            new("map_enemy_spawn_entries", "Enemy & Boss", "Enemy Spawn Entries", "map_enemy_spawn_entries", "spawn_group_id, order_index", "Danh sach enemy co the spawn trong tung group.", "Nếu 1 group có nhiều enemy thì weight quyết định xác suất tương đối khi spawn."),
            new("map_instance_configs", "Enemy & Boss", "Map Instance Configs", "map_instance_configs", "map_template_id", "Config vong doi runtime cua map instance.", "Chỉ cần cho các map instance thật sự. duration_seconds dùng cho instance đếm ngược, idle_destroy_seconds dùng cho instance farm solo."),
            new("map_templates", "World", "Map Templates", "map_templates", "id", "Map template co the chinh ngay trong admin.", "Map template la lop tong.\r\nSau khi tao map, tao tiep map zone slots de gan noi dung theo tung zone."),
            new("map_zone_slots", "World", "Map Zone Slots", "map_zone_slots", "map_template_id, zone_index", "Zone slot va linh khi theo zone.", "Moi zone slot tro den map_template va spiritual_energy_template neu co.\r\nzone_index nen khong bi trung trong cung map."),

            new("characters", "Player Runtime", "Characters", "characters", "name, created_at", "Danh sách nhân vật thật trong DB để seed dữ liệu runtime cho test.", "Thường dùng làm bảng cha cho workspace item theo nhân vật. Có thể lọc nhanh theo tên nhân vật."),
            new("player_items", "Player Runtime", "Player Items", "player_items", "player_id, id", "Item instance thật của từng nhân vật.", "Mỗi dòng là một item instance thật. Nếu là item stackable thì quantity có thể > 1. Với item trên đất, player_id sẽ để null và location_type = Ground."),
            new("player_equipments", "Player Runtime", "Player Equipments", "player_equipments", "player_item_id", "Dữ liệu instance riêng cho item equipment, gồm slot đang mặc, cường hóa và độ bền.", "Mỗi dòng bám theo đúng một player_item là equipment. Hãy tạo player_item trước, rồi mới tạo player_equipment tương ứng nếu cần."),
            new("player_equipment_stat_bonuses", "Player Runtime", "Player Equipment Stat Bonuses", "player_equipment_stat_bonuses", "player_item_id, id", "Bonus stat riêng theo từng món equipment instance.", "Dùng cho các case roll riêng, đột biến, refine hoặc event bonus. Các dòng ở đây bám theo player_item_id, không bám theo item_template."),

            new("future_bosses", "Mo Rong Sau Nay", "Bosses & Drop Tables", "monster_templates", null, "Cho nay la diem mo rong. Khi schema boss/drop co, chi can khai bao them resource de edit trong tool.", "MVP hien moi dat san navigation cho boss/drop.\r\nKhi server co bang monster_templates, monster_drop_tables, drop_entries thi tool co the mo rong theo cung co che nay.")
        ];
    }
}
