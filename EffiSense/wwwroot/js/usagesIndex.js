$(document).ready(function () {
    $("#filterDate").change(function () {
        const selectedDate = $(this).val();
        if (selectedDate) {
            localStorage.setItem("selectedDate", selectedDate);
        } else {
            localStorage.removeItem("selectedDate");
        }

        $("#usageCardContainer").fadeOut(200, function () {
            $.ajax({
                url: '/Usages/FilterByDate',
                data: { date: selectedDate },
                success: function (data) {
                    $("#usageCardContainer").html(data).fadeIn(300);
                },
                error: function () {
                    alert("Error fetching data.");
                }
            });
        });
    });

    const storedDate = localStorage.getItem("selectedDate");
    if (storedDate) {
        $("#filterDate").val(storedDate);
    }
});
