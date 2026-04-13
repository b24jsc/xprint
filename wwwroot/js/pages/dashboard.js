$(function () {
    // Lấy dữ liệu đã được bắn từ C# ra biến global
    var dashboardData = window.dashboardRawData || [];

    // BỘ MÀU CUSTOM XANH LÁ (Khớp với thiết kế)
    var greenPalette = ["#1a5d3a", "#2e7d32", "#4caf50", "#81c784", "#c8e6c9"];

    // 1. BIỂU ĐỒ TRÒN (Chuyển thành Doughnut giống ảnh thiết kế)
    $("#pieChart").dxPieChart({
        dataSource: dashboardData,
        palette: greenPalette,
        type: "doughnut", // Đổi thành vành khuyên
        innerRadius: 0.65, // Độ mỏng của vành khuyên
        series: [{
            argumentField: "TenantId",
            valueField: "TotalReports",
            label: {
                visible: true,
                position: "columns",
                connector: { visible: true, width: 1 },
                customizeText: function (arg) {
                    return arg.valueText + " mẫu";
                }
            }
        }],
        title: {
            text: "", // Đã có title ở HTML nên ẩn ở đây cho sạch
        },
        legend: {
            horizontalAlignment: "center",
            verticalAlignment: "bottom",
            itemTextPosition: "right"
        },
        tooltip: {
            enabled: true,
            format: "fixedPoint",
            customizeTooltip: function (arg) {
                return {
                    text: arg.argumentText + "<br/>" + arg.valueText + " báo cáo"
                };
            }
        }
    });

    // 2. BẢNG DỮ LIỆU (Được bo tròn và bỏ viền thô cứng)
    $("#gridContainer").dxDataGrid({
        dataSource: dashboardData,
        showBorders: false, // Bỏ viền để hòa vào card
        rowAlternationEnabled: true,
        hoverStateEnabled: true,
        columns: [
            {
                dataField: "TenantId",
                caption: "Khách Hàng (Tenant)",
                alignment: "left",
                cssClass: "fw-bold text-light"
            },
            {
                dataField: "TotalReports",
                caption: "Số Mẫu In",
                alignment: "center",
                width: 120
            },
            {
                dataField: "LastUpdated",
                caption: "Cập Nhật Cuối",
                dataType: "datetime",
                format: "dd/MM/yyyy HH:mm",
                alignment: "right",
                width: 180
            }
        ],
        searchPanel: {
            visible: true,
            placeholder: "Tìm kiếm...",
            width: 250
        },
        paging: { pageSize: 8 } // Giảm xuống 8 để bảng không quá dài, vừa vặn thẻ
    });
});