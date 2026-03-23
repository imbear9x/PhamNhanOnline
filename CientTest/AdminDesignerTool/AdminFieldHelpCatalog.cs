using System.Data;

namespace AdminDesignerTool;

internal static class AdminFieldHelpCatalog
{
    public static string BuildHelpText(
        AdminResourceDefinition resource,
        DataColumn column,
        AdminColumnBinding? binding)
    {
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
            "table_id" => "Ý nghĩa: mã bảng random mà runtime sẽ gọi tới. Nên đặt có namespace rõ ràng. Ví dụ: `monster.drop.demo_slime`.",
            "entry_id" => "Ý nghĩa: mã entry trong một bảng random. Nên ổn định, không trùng trong cùng bảng. Ví dụ: `item.demo_herb`.",
            "order_index" => "Ý nghĩa: thứ tự hiển thị hoặc thứ tự xử lý. Nên đánh tăng dần. Ví dụ: `1`, `2`, `3`.",
            "chance_parts_per_million" => "Ý nghĩa: tỷ lệ theo thang 1.000.000. Ví dụ: `50000 = 5%`, `100000 = 10%`, `250000 = 25%`.",
            "stat_type" => "Ý nghĩa: loại chỉ số bị ảnh hưởng. Ví dụ: `Hp`, `Attack`, `Speed`.",
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
            "max_stack" => "Ý nghĩa: số lượng tối đa trong một stack item. Ví dụ: `1` với trang bị, `99` với nguyên liệu.",
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
