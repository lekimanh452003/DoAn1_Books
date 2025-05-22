
var dataTable
$(document).ready(function () {
    var url = window.location.search;
    if (url.includes("inprocess")) {
        loadDataTable("inprocess");
    } else if (url.includes("paymentpending")) {
        loadDataTable("paymentpending");
    } else if (url.includes("completed")) {
        loadDataTable("completed");
    } else if (url.includes("approved")) {
        loadDataTable("approved");
    } else {
        loadDataTable("all");
    }
});
function loadDataTable() {
    dataTable=$('#tblData').DataTable({
        "ajax": {
            url: '/admin/order/getall?status='+status,
            dataSrc: "data" // Mặc định là data
        },
        "columns": [
            { data: 'id', "width": "15%" },
            { data: 'fullName', "width": "15%" },
            { data: 'phoneNumber', "width": "10%" },
            { data: 'applicationUser.email', "width": "25%" },
            { data: 'address', "width": "10%" },
            { data: 'orderStatus', "width": "10%" }, 
            { data: 'orderTotal', "width": "10%" }, 

            {
                data: null,
                className: "text-end",
                render: function (data, type, row) {
                    return `
                               <div class="d-flex gap-2"> <a href="/admin/Order/Detail?orderId=${row.id}" class="btn-action btn-edit"><i class="bi bi-pencil-square"></i>Sửa</a>
                                 </div>
                            `;
                },
                "width": "20%"
            }
        ]
    });
}
