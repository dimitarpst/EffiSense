$(document).ready(function () {
    // Date Filtering on Index
    $("#filterDate").change(function () {
        const selectedDate = $(this).val();
        if (selectedDate) {
            localStorage.setItem("selectedDate", selectedDate);
        } else {
            localStorage.removeItem("selectedDate");
        }

        $.ajax({
            url: '/Usages/FilterByDate',
            data: { date: selectedDate },
            success: function (data) {
                $("#usageTableBody").html(data);
            },
            error: function () {
                alert("Error fetching data.");
            }
        });

    });

    const storedDate = localStorage.getItem("selectedDate");
    if (storedDate) {
        $("#filterDate").val(storedDate);
    }
});
