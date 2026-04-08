using System.Data;

namespace AdminDesignerTool;

internal static class AdminFieldHelpCatalog
{
    public static string BuildHelpText(
        AdminResourceDefinition resource,
        DataColumn column,
        AdminColumnBinding? binding)
    {
        if (string.Equals(column.ColumnName, "ratio_value", StringComparison.OrdinalIgnoreCase))
        {
            return string.Join(Environment.NewLine,
            [
                $"Truong `{column.ColumnName}` trong bang `{resource.TableName}`.",
                "Y nghia: gia tri ti le dang multiplier, khong phai phan tram thuan.",
                "Quy uoc thong nhat: `0.25 = 25%`, `1.0 = 100%`, `1.1 = 110%`, `2.0 = 200%`.",
                "Khi viet `description_template`, voi field nay hay uu tien format `ratio_percent` de hien thi dung."
            ]);
        }

        if (string.Equals(column.ColumnName, "description_template", StringComparison.OrdinalIgnoreCase))
        {
            return string.Join(Environment.NewLine,
            [
                $"Truong `{column.ColumnName}` trong bang `{resource.TableName}`.",
                "Y nghia: template mo ta runtime se duoc server compile thanh text cuoi gui cho client.",
                "Co the dung token nhu `{effects_summary}`, `{qi_summary}`, `{equipment_stats_summary}` tuy loai du lieu.",
                "Khong nen dua cast time, cooldown, range vao description neu UI da co o hien thi rieng cho cac field do.",
                "Skill co the author theo token presentation nhu `{effect1.ratio_value|ratio_percent}`, `{effect1.formula_subject_rich}`, `{effect1.target_label}`.",
                "Voi field ratio nhu `ratio_value`, hay dung `|ratio_percent` de `1.1` hien thanh `110%`.",
                "Neu de trong, he thong se fallback sang `description` cu hoac template mac dinh phia server."
            ]);
        }

        if (TryGetSpecificHelp(resource.TableName, column.ColumnName, out var specific))
            return specific;

        var lines = new List<string>
        {
            $"Trường `{column.ColumnName}` trong bảng `{resource.TableName}`.",
            binding?.HeaderText is { Length: > 0 }
                ? $"Tên hiển thị: {binding.HeaderText}."
                : $"Tên hiển thị: {AdminColumnBindingCatalog.ToHeaderText(column.ColumnName)}."
        };

        if (!column.AllowDBNull && !column.ReadOnly)
            lines.Add("Bắt buộc phải nhập.");

        if (binding?.EnumType is not null)
            lines.Add($"Cách nhập: chọn một giá trị trong danh sách enum `{binding.EnumType.Name}`.");
        else if (!string.IsNullOrWhiteSpace(binding?.LookupSql))
            lines.Add("Cách nhập: chọn từ danh sách liên kết, không nên tự nhớ và gõ ID.");
        else if (column.DataType == typeof(bool))
            lines.Add("Kiểu dữ liệu: Có/Không.");
        else if (column.DataType == typeof(DateTime))
            lines.Add("Kiểu dữ liệu: ngày giờ. Thường có thể giữ giá trị mặc định do hệ thống tạo.");
        else if (column.DataType == typeof(string))
            lines.Add("Kiểu dữ liệu: văn bản.");
        else
            lines.Add($"Kiểu dữ liệu: {column.DataType.Name}.");

        lines.Add(GetGenericHint(column.ColumnName));
        return string.Join(Environment.NewLine, lines.Where(static x => !string.IsNullOrWhiteSpace(x)));
    }

    private static bool TryGetSpecificHelp(string tableName, string columnName, out string helpText)
    {
        var key = $"{tableName}.{columnName}";
        if (SpecificHelp.TryGetValue(key, out helpText!))
            return true;

        return SpecificHelp.TryGetValue(columnName, out helpText!);
    }

    private static string GetGenericHint(string columnName)
    {
        return columnName switch
        {
            "id" => "Ý nghĩa: khóa chính của bản ghi. Tool sẽ gợi ý số tiếp theo, nhưng bạn vẫn có thể sửa nếu team có quy ước riêng. Ví dụ: `101`.",
            "code" => "Ý nghĩa: mã định danh ổn định để code và data tham chiếu. Nên dùng chữ thường, không dấu, ngăn cách bằng dấu gạch dưới. Ví dụ: `thiet_moc_kiem`.",
            "name" => "Ý nghĩa: tên hiển thị cho game design và UI. Nên đặt ngắn gọn, dễ hiểu. Ví dụ: `Thiết Mộc Kiếm`.",
            "description" => "Ý nghĩa: mô tả nghiệp vụ hoặc ghi chú cho người thiết kế. Có thể viết tự do để người khác hiểu bản ghi này dùng để làm gì.",
            "created_at" => "Ý nghĩa: thời điểm tạo bản ghi theo giờ UTC. Tool và server sẽ tự điền khi tạo mới, bạn thường không cần nhập tay.",
            "updated_at" => "Ý nghĩa: thời điểm cập nhật gần nhất theo giờ UTC. Tool và server sẽ tự cập nhật khi bản ghi thay đổi.",
            "item_template_id" => "Ý nghĩa: trỏ đến Item Template cha. Nếu danh sách trống, hãy tạo Item Template phù hợp trước.",
            "martial_art_id" => "Ý nghĩa: trỏ đến Công Pháp cha.",
            "martial_art_stage_id" => "Ý nghĩa: trỏ đến một tầng Công Pháp cụ thể.",
            "skill_id" => "Ý nghĩa: trỏ đến Skill cha.",
            "skill_effect_id" => "Ý nghĩa: trỏ đến một hiệu ứng cụ thể của skill.",
            "craft_recipe_id" => "Ý nghĩa: trỏ đến công thức chế tạo cha.",
            "pill_recipe_template_id" => "Ý nghĩa: trỏ đến đan phương cha.",
            "pill_template_id" => "Ý nghĩa: trỏ đến pill template cha.",
            "herb_template_id" => "Ý nghĩa: trỏ đến herb template cha.",
            "game_random_table_id" => "Ý nghĩa: trỏ đến bảng random cha.",
            "game_random_entry_id" => "Ý nghĩa: trỏ đến entry random cha.",
            "map_template_id" => "Ý nghĩa: trỏ đến Map Template cha.",
            "spiritual_energy_template_id" => "Ý nghĩa: trỏ đến template linh khí dùng cho map hoặc zone.",
            "player_id" => "Ý nghĩa: nhân vật sở hữu item instance này. Nếu item đang ở dưới đất thì trường này thường để trống.",
            "player_item_id" => "Ý nghĩa: mã của item instance thật. Các bảng equipment instance và bonus stat riêng đều phải bám theo player_item_id này.",
            "table_id" => "Ý nghĩa: mã bảng random mà runtime sẽ gọi tới. Nên đặt có namespace rõ ràng. Ví dụ: `monster.drop.demo_slime`.",
            "entry_id" => "Ý nghĩa: mã entry trong một bảng random. Nên ổn định, không trùng trong cùng bảng. Ví dụ: `item.demo_herb`.",
            "order_index" => "Ý nghĩa: thứ tự hiển thị hoặc thứ tự xử lý. Nên đánh tăng dần. Ví dụ: `1`, `2`, `3`.",
            "chance_parts_per_million" => "Ý nghĩa: tỷ lệ theo thang 1.000.000. Ví dụ: `50000 = 5%`, `100000 = 10%`, `250000 = 25%`.",
            "stat_type" => "Ý nghĩa: loại chỉ số tổng/nội tại bị ảnh hưởng. Ví dụ: `MaxHp`, `Attack`, `Speed`. Không dùng trường này cho HP/MP/Stamina hiện tại trong combat.",
            "value_type" => "Ý nghĩa: cách hiểu giá trị, ví dụ cộng thẳng, phần trăm, tỷ lệ.",
            "formula_type" => "Ý nghĩa: công thức tính hiệu ứng của skill. Ví dụ: theo hệ số Attack hoặc cộng thẳng.",
            "target_type" => "Ý nghĩa: kiểu mục tiêu mà skill hoặc effect tác động vào. Ví dụ: bản thân, một địch, vùng.",
            "consume_mode" => "Ý nghĩa: cách hệ thống tiêu hao nguyên liệu khi chế tạo hoặc luyện đan.",
            "required_quantity" => "Ý nghĩa: số lượng vật phẩm cần cho một lần dùng. Ví dụ: `3`.",
            "required_growth_seconds" => "Ý nghĩa: tổng thời gian tăng trưởng tích lũy để đạt mốc này. Ví dụ: `3600` là khoảng 1 giờ.",
            "result_item_template_id" => "Ý nghĩa: Item Template của vật phẩm đầu ra sau khi chế tạo.",
            "result_pill_item_template_id" => "Ý nghĩa: Item Template của viên đan tạo ra sau khi luyện.",
            "recipe_book_item_template_id" => "Ý nghĩa: Item Template của sách hoặc đan phương dùng để học công thức.",
            "seed_item_template_id" => "Ý nghĩa: Item Template của hạt giống để trồng herb này.",
            "replant_item_template_id" => "Ý nghĩa: Item Template của cây sống có thể bỏ vào túi rồi trồng lại. Nên chọn item loại HerbPlant.",
            "growth_speed_rate" => "Ý nghĩa: hệ số tốc độ tăng trưởng của đất. Ví dụ: `1.0` là bình thường, `1.5` là nhanh hơn 50%.",
            "max_active_seconds" => "Ý nghĩa: tổng số giây đất còn hiệu lực. Ví dụ: `86400` là 1 ngày.",
            "success_rate" => "Ý nghĩa: tỷ lệ thành công cơ bản. Hãy xem đúng quy ước của bảng này trước khi nhập.",
            "mutation_rate" => "Ý nghĩa: tỷ lệ đột biến cơ bản của công thức.",
            "mutation_rate_cap" => "Ý nghĩa: trần trên của tỷ lệ đột biến sau khi cộng bonus.",
            "cooldown_ms" => "Ý nghĩa: thời gian hồi chiêu tính bằng mili giây. Ví dụ: `1500` là 1,5 giây.",
            "cast_range" => "Ý nghĩa: tầm thi triển thực tế của skill để client và AI biết cần đứng gần tới đâu mới dùng được. Ví dụ: `1.5` cho cận chiến, `6.0` cho chưởng tầm trung, `10.0` cho bắn xa.",
            "skill_group_code" => "Ý nghĩa: mã nhóm để gom các cấp của cùng một skill. Ví dụ: `truong_xuan_chuong` cho cấp 1, 2, 3 của cùng chiêu thức.",
            "skill_level" => "Ý nghĩa: cấp của skill trong nhóm. Ví dụ: `1`, `2`, `3`. Mô hình hiện tại là mỗi cấp skill là một row riêng trong bảng `skills`.",
            "max_stack" => "Ý nghĩa: số lượng tối đa trong một stack item. Ví dụ: `1` với trang bị, `99` với nguyên liệu.",
            "location_type" => "Ý nghĩa: item instance hiện đang ở ngữ cảnh nào như Inventory, Ground, Mail hay Storage.",
            "equipped_slot" => "Ý nghĩa: ô trang bị đang mặc của item equipment. Nếu để trống thì món đó chỉ đang nằm trong túi.",
            "source_type" => "Ý nghĩa: nguồn gốc của bonus stat riêng theo từng món, ví dụ drop bonus, craft bonus, mutation bonus hay refine bonus.",
            _ => "Hãy nhập giá trị đúng theo nghiệp vụ của bảng này. Nếu đây là trường liên kết thì ưu tiên chọn từ dropdown thay vì gõ tay ID."
        };
    }

    private static readonly IReadOnlyDictionary<string, string> SpecificHelp =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["item_templates.id"] = "Đây là mã số chính của Item Template.\nVí dụ: `1001`.\nKhuyến nghị: chia các dải ID theo nhóm item để sau này dễ quản lý, ví dụ 1000-1999 cho vũ khí, 2000-2999 cho đan dược.",
            ["item_templates.code"] = "Mã logic của item để code và data tham chiếu.\nVí dụ: `kiem_sat`, `dan_hoi_linh`, `hat_giong_huyet_sam`.\nKhuyến nghị: dùng chữ thường, không dấu, ngăn cách bằng `_`, không đổi tùy tiện sau khi đã dùng trong data khác.",
            ["item_templates.name"] = "Tên hiển thị của item trong tool và UI.\nVí dụ: `Kiếm Sắt`, `Hồi Linh Đan`, `Hạt Giống Huyết Sâm`.",
            ["item_templates.icon"] = "Đường dẫn icon hoặc mã icon dùng cho client.\nVí dụ: `icons/items/kiem_sat.png` hoặc `item_kiem_sat` tùy quy ước project.\nĐây là ô văn bản tự do, có thể nhập chuỗi dài hơn 1 ký tự.",
            ["item_templates.background_icon"] = "Mã background/icon nền của item dùng cho client UI.\nVí dụ: `bg_item_epic`, `bg_item_pill`, `bg_item_weapon_green`.\nNếu để trống, client có thể fallback theo rarity hoặc item_type.",
            ["item_templates.description"] = "Mô tả nội dung, công dụng hoặc ghi chú cho item.\nVí dụ: `Thanh kiếm sắt phổ thông dành cho tân thủ.`\nĐây là ô văn bản tự do, có thể nhập mô tả dài nhiều câu.",
            ["item_templates.item_type"] = "Loại item gốc.\nVí dụ: `Equipment` cho trang bị, `Consumable` cho vật phẩm dùng trực tiếp, `HerbSeed` cho hạt giống, `HerbPlant` cho cây sống có thể trồng lại, `Soil` cho linh thổ.\nLưu ý: nhiều bảng con phụ thuộc trực tiếp vào trường này, nên cần chọn đúng ngay từ đầu.",
            ["item_templates.rarity"] = "Phẩm chất của item.\nVí dụ: `Common`, `Rare`, `Epic`.\nDùng để cân bằng và hiển thị độ hiếm trong UI.",
            ["item_templates.max_stack"] = "Số lượng tối đa trong một stack.\nVí dụ: trang bị thường là `1`, nguyên liệu có thể là `99` hoặc `999`.\nNếu là `Equipment`, `Soil` hoặc `HerbPlant` thì thường nên để `1`.",
            ["item_templates.is_tradeable"] = "Có cho phép giao dịch giữa người chơi hay không.\nVí dụ: bật nếu item được phép mua bán.",
            ["item_templates.is_droppable"] = "Có cho phép ném hoặc rơi ra thế giới hay không.",
            ["item_templates.is_destroyable"] = "Có cho phép hủy item khỏi inventory hay không.",

            ["characters.account_id"] = "Tài khoản sở hữu nhân vật này.\nTrong tool đã có lookup sang tên đăng nhập hoặc credential để dễ đọc hơn UUID gốc.",

            ["player_items.player_id"] = "Nhân vật sở hữu item instance này.\nNếu item đang ở túi hoặc đang mặc thì phải trỏ tới đúng character.\nNếu item đang ở dưới đất thì thường để trống.",
            ["player_items.item_template_id"] = "Template gốc của item instance.\nVí dụ: cùng là `Kim Huyền Kiếm` thì nhiều player_item khác nhau vẫn có thể cùng trỏ về một item_template_id.",
            ["player_items.location_type"] = "Vị trí logic hiện tại của item instance.\nDùng `Inventory` cho item trong túi, `Ground` cho item đang nằm dưới đất, các loại khác dành cho phase sau như mail hoặc storage.",
            ["player_items.quantity"] = "Số lượng của item instance.\nVới equipment thường là `1`.\nVới item stackable như linh thạch hay nguyên liệu có thể lớn hơn 1.",
            ["player_items.is_bound"] = "Đánh dấu item có khóa giao dịch hay không.\nNếu bật thì về sau có thể bị chặn trade hoặc mail tùy luật runtime.",
            ["player_items.expire_at"] = "Thời điểm item tự hết hạn nếu có.\nĐể trống nếu item tồn tại vĩnh viễn.",

            ["player_equipments.player_item_id"] = "Chọn đúng player_item là equipment để gắn dữ liệu instance.\nMột món equipment trong túi hoặc đang mặc đều nên có tối đa một dòng ở bảng này.",
            ["player_equipments.equipped_slot"] = "Ô đang mặc của món đồ.\nĐể trống nếu đồ chỉ đang nằm trong túi.\nĐiền `Weapon`, `Armor`, `Pants`, `Shoes` nếu muốn seed sẵn trạng thái đang mặc.",
            ["player_equipments.enhance_level"] = "Mức cường hóa hiện tại của món đồ.\nVí dụ: `0` là chưa cường hóa, `5` là +5.",
            ["player_equipments.durability"] = "Độ bền hiện tại của món đồ nếu game design muốn test UI hoặc logic hao mòn.\nCó thể để trống nếu phase hiện tại chưa dùng.",

            ["player_equipment_stat_bonuses.player_item_id"] = "Item instance nhận bonus stat riêng.\nĐây là chìa khóa để hai món cùng template vẫn có stat khác nhau.",
            ["player_equipment_stat_bonuses.stat_type"] = "Loại chỉ số tổng được cộng riêng cho món này.\nVí dụ: `Attack`, `MaxHp`, `Speed`.",
            ["player_equipment_stat_bonuses.value"] = "Giá trị bonus riêng của món.\nVí dụ: `10` nếu muốn cộng thêm 10 Attack khi value_type là Flat.",
            ["player_equipment_stat_bonuses.value_type"] = "Cách hiểu giá trị bonus.\nĐể test nhanh, nên dùng `Flat` trước cho dễ kiểm tra trên UI và runtime.",
            ["player_equipment_stat_bonuses.source_type"] = "Nguồn của bonus riêng.\nVí dụ: `DropBonus` cho đồ rơi biến dị, `CraftBonus` cho đồ chế tạo, `RefineBonus` cho luyện hóa.",

            ["pill_templates.item_template_id"] = "Chọn item đại diện cho viên đan này.\nVí dụ: chọn Item Template `hoi_linh_dan`.\nĐiều kiện: trước đó phải có Item Template phù hợp, thường là loại `Consumable`.",
            ["pill_templates.pill_category"] = "Nhóm đan dược.\nVí dụ: `Recovery` cho đan hồi phục, `Buff` cho đan tăng chỉ số, `Breakthrough` cho đan hỗ trợ đột phá.",
            ["pill_templates.usage_type"] = "Cách dùng của viên đan.\nVí dụ: `ConsumeDirectly` là uống trực tiếp; `PassiveMaterial` là chỉ làm nguyên liệu, chưa dùng như consumable.",
            ["pill_effects.effect_type"] = "Loại hiệu ứng của viên đan.\nVí dụ: `RecoverHp`, `RecoverMp`, `AddBuffStat`.\nNếu là đan hồi phục thì thường dùng `RecoverHp` hoặc `RecoverMp`.",
            ["pill_effects.base_value"] = "Giá trị cơ bản của hiệu ứng.\nVí dụ: hồi 150 HP thì nhập `150`.\nHãy kết hợp đúng với `effect_type` và `value_type`.",

            ["pill_recipe_templates.code"] = "Mã đan phương.\nVí dụ: `dan_phuong_hoi_linh_dan`.\nKhuyến nghị: đặt gần với tên viên đan kết quả để dễ tìm.",
            ["pill_recipe_templates.name"] = "Tên hiển thị của đan phương.\nVí dụ: `Đan Phương Hồi Linh Đan`.",
            ["pill_recipe_templates.recipe_book_item_template_id"] = "Chọn Item Template của sách hoặc đan phương dùng để học recipe.\nVí dụ: `sach_dan_hoi_linh`.\nĐiều kiện: Item Template đó nên có loại `PillRecipeBook`.",
            ["pill_recipe_templates.result_pill_item_template_id"] = "Chọn Item Template của viên đan đầu ra.\nVí dụ: `hoi_linh_dan`.",
            ["pill_recipe_templates.base_success_rate"] = "Tỷ lệ thành công cơ bản của đan phương.\nVí dụ: `0.65` nếu bạn đang dùng quy ước 0..1.\nNên giữ nhất quán trên toàn bộ bảng.",
            ["pill_recipe_templates.mutation_rate"] = "Tỷ lệ đột biến cơ bản của đan phương.\nVí dụ: `0.05` nếu bạn muốn 5%.\nNếu chưa dùng cơ chế đột biến, có thể để `0`.",
            ["pill_recipe_inputs.required_item_template_id"] = "Nguyên liệu cần cho đan phương.\nVí dụ: `huyet_sam`, `linh_thao`, `nuoc_linh`.\nPhải chọn từ Item Template đã tồn tại.",
            ["pill_recipe_inputs.required_quantity"] = "Số lượng nguyên liệu cần cho một lần luyện.\nVí dụ: `2`, `5`, `10`.",
            ["pill_recipe_inputs.is_optional"] = "Đánh dấu nếu đây là nguyên liệu tùy chọn.\nNguyên liệu tùy chọn thường dùng để tăng tỷ lệ thành công hoặc đột biến.",
            ["pill_recipe_inputs.success_rate_bonus"] = "Bonus thêm vào tỷ lệ thành công khi dùng nguyên liệu tùy chọn này.\nVí dụ: `0.05` để cộng thêm 5%.",
            ["pill_recipe_inputs.mutation_bonus_rate"] = "Bonus thêm vào tỷ lệ đột biến khi dùng nguyên liệu tùy chọn này.",
            ["pill_recipe_inputs.required_herb_maturity"] = "Giữ chỗ cho phase sau khi craft trực tiếp từ cây đang trồng.\nHiện tại thường để `None`.",
            ["pill_recipe_mastery_stages.required_total_craft_count"] = "Tổng số lần luyện cần đạt để mở mốc thông thạo này.\nVí dụ: `10`, `50`, `100`.",
            ["pill_recipe_mastery_stages.success_rate_bonus"] = "Bonus tỷ lệ thành công nhận được khi đạt mốc thông thạo.\nVí dụ: `0.03` để cộng 3%.",
            ["skills.cast_range"] = "Tầm thi triển gốc của skill.\nVí dụ: `1.5` cho đòn chém cận chiến, `4` cho thương pháp tầm ngắn, `8` cho chưởng tầm xa.\nField này chủ yếu để client và AI xử lý khoảng cách hợp lý, server không cần validate quá chặt ở mọi frame.",
            ["skills.cast_time_ms"] = "Thời gian niệm hoặc gồng trước khi skill được phóng ra.\nVí dụ: `300` nghĩa là mất 0.3 giây để thi triển.\nNếu skill đánh ngay lập tức thì để `0`.",
            ["skills.travel_time_ms"] = "Thời gian hiệu ứng bay từ lúc phóng tới khi chạm mục tiêu.\nVí dụ: `500` nghĩa là sau 0.5 giây kể từ lúc phóng mới gây sát thương.\nVới skill cận chiến hoặc hit-scan có thể để `0`.",
            ["skills.skill_group_code"] = "Mã nhóm của skill.\nVí dụ: `truong_xuan_chuong`.\nCác row skill cùng nhóm như cấp 1, 2, 3 sẽ dùng chung skill_group_code, khác nhau ở skill_level.",
            ["skills.skill_level"] = "Cấp của skill trong nhóm.\nVí dụ: `1`, `2`, `3`.\nTheo thiết kế mới, mỗi cấp skill là một row riêng trong bảng skills và có effect riêng.",
            ["skills.skill_category"] = "Loại slot của skill.\n`Basic` là skill cơ bản bắt buộc dùng ở ô skill số 1.\n`Normal` và `Special` dùng cho các ô còn lại. Phase hiện tại `Special` có rule loadout giống `Normal`, nhưng sẽ mở rộng riêng sau.",
            ["skill_effects.effect_type"] = "Loại effect runtime của skill.\nAllowed hiện tại: `Damage`, `Heal`, `ResourceReduce`, `ResourceRestore`, `Shield`, `Stun`, `BuffStat`, `DebuffStat`.\n`resource_type` chỉ dùng cho resource effect. `stat_type` + `value_type` chỉ có nghĩa rõ với buff/debuff stat.",
            ["skill_effects.formula_type"] = "Công thức tính magnitude của effect.\nEnum chuẩn hiện tại: `Flat`, `AttackRatio`, `CasterMaxHpRatio`, `CasterMaxMpRatio`.\nVí dụ damage `20 + 2% Attack` thì dùng `formula_type = AttackRatio`, `base_value = 20`, `ratio_value = 0.02`.",
            ["skill_effects.target_scope"] = "Phạm vi áp effect của từng row effect.\nEnum chuẩn hiện tại: `Primary`, `AreaAroundPrimary`, `Self`, `AllResolvedTargets`, `AllEnemiesMap`, `AllAlliesMap`, `AllUnitsMap`.\nRuntime phase hiện tại mới support ổn `Primary` và `Self`.",
            ["skill_effects.trigger_timing"] = "Thời điểm effect được áp.\nEnum chuẩn hiện tại: `OnCastRelease`, `OnHit`, `OnExpire`, `OnCastStart`, `OnInterval`.\nRuntime phase hiện tại mới support ổn `OnCastRelease` và `OnHit`.",
            ["skill_effects.resource_type"] = "Loại tài nguyên hiện tại bị tác động khi effect là `ResourceReduce` hoặc `ResourceRestore`.\nAllowed hiện tại: `Hp`, `Mp`, `Stamina`.\nKhông dùng field này cho `MaxHp`, `MaxMp`, `MaxStamina`.",
            ["skill_effects.stat_type"] = "Loại chỉ số tổng bị tác động khi effect là `BuffStat` hoặc `DebuffStat`.\nAllowed hiện tại: `MaxHp`, `MaxMp`, `MaxStamina`, `Attack`, `Speed`, `SpiritualSense`, `Fortune`.",
            ["skill_effects.duration_ms"] = "Thời lượng effect tính bằng mili giây.\nBắt buộc với `Stun`, `BuffStat`, `DebuffStat` nếu muốn effect tồn tại theo thời gian.\nVới `Shield`, để trống nghĩa là lá chắn tồn tại tới khi bị phá hết.\nDuration chỉ là config tĩnh; buff runtime không lưu vĩnh viễn xuống DB.",
            ["skill_effects.value_type"] = "Cách hiểu giá trị của effect.\nAllowed hiện tại: `Flat`, `Ratio`, `Percent`.\nField này chủ yếu có nghĩa với `BuffStat`/`DebuffStat`.\nVới damage/heal/resource, phase hiện tại server chủ yếu đọc magnitude từ `formula_type`, `base_value`, `ratio_value`; `value_type` gần như không tham gia tính toán chính.",
            ["enemy_templates.base_move_speed"] = "Tốc độ di chuyển gốc của enemy/boss theo đơn vị logic server trên mỗi giây.\nVề sau client có thể quy đổi sang world units theo tỉ lệ map để biểu diễn movement.\nVí dụ: `100` nghĩa là 10 giây đi hết chiều ngang một map rộng 1000 đơn vị nếu không có modifier khác.",
            ["enemy_templates.enable_out_of_combat_restore"] = "Bật/tắt cơ chế enemy tự hồi đầy máu sau khi thoát combat đủ lâu.\nNên bật cho quái thường hoặc hình nộm luyện tập nếu muốn reset máu tự động.\nCó thể tắt cho boss để tránh đánh lâu rồi bị hồi full máu.",
            ["enemy_templates.out_of_combat_restore_delay_seconds"] = "Số giây kể từ lần bị đánh cuối cùng để enemy tự hồi đầy máu và quay về Patrol.\nVí dụ: `20` nghĩa là nếu 20 giây không bị đánh nữa thì sẽ reset full máu.\nTrường này chỉ có ý nghĩa khi `enable_out_of_combat_restore = true`.",
            ["player_skills.skill_group_code"] = "Nhóm skill mà nhân vật đang sở hữu.\nRuntime dùng trường này để giữ lại duy nhất một cấp hiện hành cho mỗi nhóm skill, bất kể skill đến từ học trực tiếp, công pháp hay nguồn khác.",
            ["player_skills.source_type"] = "Nguồn cấp skill cho nhân vật.\nVí dụ: `1 = cấp tay/manual`, `2 = unlock từ công pháp`, các giá trị khác dành cho item, quest hoặc system grant ở phase sau.",
            ["player_skills.source_martial_art_id"] = "Nếu skill này đến từ unlock công pháp thì trường này trỏ về công pháp nguồn.\nNếu skill được cấp độc lập thì có thể để trống.",
            ["player_skills.source_martial_art_skill_id"] = "Nếu skill này đến từ một mốc unlock cụ thể trong `martial_art_skills` thì trường này trỏ về row nguồn.\nNếu skill được cấp độc lập thì có thể để trống.",

            ["herb_templates.seed_item_template_id"] = "Item Template của hạt giống dùng để trồng mới herb này.\nVí dụ: `hat_giong_huyet_sam`.\nKhi người chơi trồng từ hạt, server sẽ map từ item này sang đúng herb template.",
            ["herb_templates.replant_item_template_id"] = "Item Template của cây sống có thể mang trong túi và trồng lại.\nVí dụ: `cay_non_huyet_sam`, `linh_thao_song`.\nDùng cho case nhổ cây non hoặc cây sống khỏi vườn, bỏ vào túi rồi sau đó trồng lại.\nKhuyến nghị: chọn Item Template có loại `HerbPlant` để dễ phân biệt với hạt giống.",

            ["game_random_tables.table_id"] = "Mã bảng random mà runtime sẽ gọi.\nVí dụ: `monster.drop.demo_slime`, `boss.drop.hac_ho`, `craft.bonus.weapon_common`.\nNên đặt ổn định, có namespace rõ ràng.",
            ["game_random_tables.mode"] = "Chế độ hoạt động của bảng random.\nHiện tại runtime mới hỗ trợ `Exclusive`, nghĩa là chọn một kết quả cuối cùng sau khi chuẩn hóa tổng tỷ lệ.",
            ["game_random_tables.fortune_enabled"] = "Bật nếu bảng random này cho phép chỉ số Vận May ảnh hưởng đến kết quả.",
            ["game_random_tables.fortune_bonus_parts_per_million_per_fortune_point"] = "Mỗi 1 điểm Vận May cộng thêm bao nhiêu phần triệu.\nVí dụ: `2500` nghĩa là mỗi 1 điểm Fortune cộng thêm 0,25%.",
            ["game_random_tables.fortune_max_bonus_parts_per_million"] = "Giới hạn trên của bonus do Vận May tạo ra.\nVí dụ: `150000` nghĩa là tối đa cộng thêm 15%.",
            ["game_random_tables.none_entry_id"] = "Mã entry đại diện cho trường hợp không rớt gì hoặc phần chance dư còn lại.\nThường để `__none__`.",
            ["game_random_entries.entry_id"] = "Mã entry của một kết quả random.\nVí dụ: `item.demo_herb`, `currency.spirit_stone_small`, `item.kiem_sat`.",
            ["game_random_entries.chance_parts_per_million"] = "Tỷ lệ của entry theo thang 1.000.000.\nVí dụ: `50000 = 5%`, `250000 = 25%`.",
            ["game_random_entries.is_none"] = "Đánh dấu đây là entry 'không rớt gì'.\nThường chỉ nên có tối đa một entry kiểu này trong mỗi bảng random.",
            ["game_random_entries.order_index"] = "Thứ tự hiển thị hoặc thứ tự đọc data.\nVí dụ: `1`, `2`, `3`.",
            ["game_random_entry_tags.tag"] = "Tag gắn cho entry để fortune modifier có thể áp dụng chọn lọc.\nVí dụ: `item_drop`, `currency_drop`, `rare_drop`.",
            ["game_random_fortune_tags.tag"] = "Danh sách tag được hưởng bonus từ Vận May.\nVí dụ: nếu thêm `item_drop` thì chỉ các entry có tag đó mới được cộng chance."
        };
}
