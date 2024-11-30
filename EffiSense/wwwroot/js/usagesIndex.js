$(document).ready(function () {
    // Date Filtering on Index
    $("#filterDate").change(function () {
        const selectedDate = $(this).val();
        if (selectedDate) {
            localStorage.setItem("selectedDate", selectedDate);
        } else {
            localStorage.removeItem("selectedDate"); // Clear stored date if input is cleared
        }

        $.ajax({
            url: '/Usages/FilterByDate',
            data: { date: selectedDate || null }, // Send null if no date is selected
            success: function (data) {
                $("#usageTableBody").html(data);
            },
            error: function () {
                alert("Error fetching data.");
            }
        });
    });

    // Populate the date filter with the stored date on page load
    const storedDate = localStorage.getItem("selectedDate");
    if (storedDate) {
        $("#filterDate").val(storedDate);
    }
});
