$(function () {
    const csrfToken = $('meta[name="csrf-token"]').attr('content');

    // Bắt kích thước file
    $(document).on('change', '#fileUploadInput', function () {
        const file = this.files[0];
        if (file) {
            const sizeKB = (file.size / 1024).toFixed(2);
            $('#fileSizeInfo').html(`<i class="dx-icon-check"></i> ${file.name} (${sizeKB} KB)`).fadeIn();
        } else {
            $('#fileSizeInfo').hide();
        }
    });

    // Khởi tạo dxButton cho nút Hủy
    $("#btnCancelImport").dxButton({
        text: "Hủy",
        type: "normal",
        onClick: function () {
            window.importPopup.hide();
        }
    });

    // Khởi tạo dxButton cho nút Upload
    var btnUpload = $("#btnUploadAjax").dxButton({
        text: "Import File",
        icon: "upload",
        type: "default",
        onClick: function () {
            const formElement = $('#frmImportFile')[0];
            if (!formElement.checkValidity()) {
                formElement.reportValidity();
                return;
            }

            const formData = new FormData(formElement);
            formData.append('__RequestVerificationToken', csrfToken);

            btnUpload.option("text", "Đang tải...");
            btnUpload.option("disabled", true);

            $.ajax({
                url: '/Reports/Import', type: 'POST', data: formData, processData: false, contentType: false,
                success: function (res) {
                    window.importPopup.hide();
                    formElement.reset();
                    $('#fileSizeInfo').hide();
                    DevExpress.ui.notify(res, "success", 2000);
                    if (window.reloadGridData) window.reloadGridData();
                },
                error: function (xhr) {
                    DevExpress.ui.notify("Lỗi: " + xhr.responseText, "error", 3000);
                },
                complete: function () {
                    btnUpload.option("text", "Import File");
                    btnUpload.option("disabled", false);
                }
            });
        }
    }).dxButton("instance");
});