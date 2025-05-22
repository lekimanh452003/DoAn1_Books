
var dataTable
$(document).ready(function () {
    loadDataTable();
});

function loadDataTable() {
    dataTable = $('#tblData').DataTable({
        "ajax": {
            url: '/admin/product/getall',
            dataSrc: "data"
        },
        "columns": [
            { data: 'title', "width": "20%", title: "Tiêu đề" },
            { data: 'author', "width": "15%", title: "Tác giả" },
            {
                data: 'category.categoryName',
                "width": "15%",
                title: "Thể loại"
            },
            {
                data: 'price',
                "width": "10%",
                render: $.fn.dataTable.render.number(',', '.', 0),
                title: "Giá"
            },
            {
                data: 'releaseDate',
                "width": "15%",
                render: function (data) {
                    return data ? data.split('T')[0] : '';
                },
                title: "Ngày phát hành"
            },
            {
                data: 'description',
                "width": "25%",
                render: function (data) {
                    // Hiển thị ngắn gọn mô tả
                    return data && data.length > 60 ? data.substring(0, 60) + "..." : data;
                },
                title: "Mô tả"
            },
            {
                data: null,
                className: "text-end",
                render: function (data, type, row) {
                    return `
                        <div class="d-flex gap-2">
                            <a href="/admin/Product/Upsert/${row.id}" class="btn-action btn-edit">
                                <i class="bi bi-pencil-square"></i> Sửa
                            </a>
                            <button onclick="Delete('/admin/product/Delete/${row.id}')" class="btn-action btn-delete">
                                <i class="bi bi-trash"></i> Xóa
                            </button>
                        </div>
                    `;
                },
                "width": "20%",
                title: "Thao tác"
            }
        ]
    });
}

function Delete(url) {
    Swal.fire({
        title: "Bạn chắc chắn xóa?",
        text: "Hành động này không thể hoàn tác!",
        icon: "warning",
        showCancelButton: true,
        confirmButtonColor: "#3085d6",
        cancelButtonColor: "#d33",
        confirmButtonText: "Xóa ngay!",
        cancelButtonText:"Hủy bỏ"
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                url: url,
                type: 'DELETE',
                success: function (data) {
                    toastr.success(data.message);
                    dataTable.ajax.reload();
                },
                error: function () {
                    toastr.error("Xóa không thành công");
                }
            });
        }
    });
}