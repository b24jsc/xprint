$(function () {
    // --- 1. DỮ LIỆU MENU ---
    var menuData = [
        { id: "1", text: "Dashboard", icon: "home", url: appUrls.homeIndex },
        {
            id: "2", text: "Reports", icon: "chart", expanded: true, items: [
                { id: "2_1", text: "Manager", icon: "folder", url: appUrls.reportsIndex },
                { id: "2_2", text: "Design", icon: "edit", url: appUrls.reportsDesign },
                { id: "2_3", text: "View", icon: "doc", url: appUrls.reportsView }
            ]
        },
        {
            id: "3", text: "Cấu hình", icon: "preferences", items: [
                { id: "3_1", text: "Quản lý User", icon: "group", url: '@Url.Action("Index", "User")' },
                { id: "3_2", text: "Cấu hình API", icon: "globe", url: '@Url.Action("Api", "Settings")' },
                { id: "3_3", text: "Kết nối Database", icon: "data", url: '@Url.Action("Database", "Settings")' }
            ]
        }
    ];

    // --- 2. KHỞI TẠO DRAWER ---
    var drawer = $("#app-drawer").dxDrawer({
        opened: true,
        openedStateMode: "shrink", 
        position: "left",
        revealMode: "slide",      
        template: function () {
            var $treeView = $("<div>").dxTreeView({
                dataSource: menuData,
                selectionMode: "single",
                focusStateEnabled: false,
                hoverStateEnabled: true,
                expandEvent: "click",
                width: 250, 
                onItemClick: function (e) {
                    if (e.itemData.url) {
                        window.location.href = e.itemData.url;
                    }
                }
            });
            return $("<div>").addClass("sidebar-menu").append($treeView);
        }
    }).dxDrawer("instance");

    // --- 3. KHỞI TẠO TOOLBAR ---
    var isAuth = '@User.Identity.IsAuthenticated'.toLowerCase() === 'true';
    var userName = '@(User.Identity.IsAuthenticated ? User.Identity.Name : "")';

    var toolbarItems = [
        {
            widget: "dxButton",
            location: "before",
            options: {
                icon: "menu",
                stylingMode: "text",
                onClick: function () {
                    drawer.toggle();
                }
            }
        },
        {
            location: "before",
            template: function () {
                return $("<a href='/' class='header-logo'>")
                    .append("<img src='/lib/img/xbook-icon.svg' width='28' height='28' alt='Logo' />")
                    .append("<span>XPRINT</span>");
            }
        }
    ];

    if (isAuth) {
        toolbarItems.push({
            location: "after",
            template: function () {
                return $("<div style='padding-right: 15px; color: #bbb;'>")
                    .append("Xin chào, ")
                    .append($("<span style='color: #00d2ff; font-weight: bold;'>").text(userName));
            }
        });
        toolbarItems.push({
            widget: "dxButton",
            location: "after",
            options: {
                text: "Đăng xuất",
                icon: "runner",
                type: "danger",
                stylingMode: "outlined",
                onClick: function () {
                    window.location.href = '@Url.Action("Logout", "Auth")';
                }
            }
        });
    }

    $("#toolbar").dxToolbar({
        items: toolbarItems
    });

    // --- 4. TỰ ĐỘNG CHỌN MENU HIỆN TẠI ---
    // Tự động highlight item trên menu dựa vào URL đang truy cập
    var currentUrl = window.location.pathname;
    var treeView = $(".sidebar-menu > div").dxTreeView("instance");
    if (treeView) {
        $.each(menuData, function (_, group) {
            if (group.url && currentUrl.toLowerCase().includes(group.url.toLowerCase())) {
                treeView.selectItem(group.id);
            }
            if (group.items) {
                $.each(group.items, function (_, item) {
                    if (item.url && currentUrl.toLowerCase().includes(item.url.toLowerCase())) {
                        treeView.selectItem(item.id);
                    }
                });
            }
        });
    }

    // Xóa padding giữ chỗ của CSS cứng, trả lại quyền kiểm soát cho DevExtreme
    $("#app-drawer").css("padding-left", "");

    // Kéo màn che lên sau khi mọi thứ đã render xong
    $("body").removeClass("dx-cloak").addClass("dx-cloak-removed");
});