$(document).ready(function () {
    const selectedDate = localStorage.getItem("selectedDate");
    const usageDateInput = $("#usageDate");

    if (selectedDate) {
        usageDateInput.val(selectedDate);
    }

    $("#createUsageForm").on("submit", function (e) {
        if (!usageDateInput.val()) {
            alert("Please select a date before creating a usage.");
            e.preventDefault();
        }
    });

    $("#HomeId").change(function () {
        var homeId = $(this).val();
        var applianceDropdown = $("#ApplianceId");

        applianceDropdown.empty().append('<option value="">Select Appliance</option>');

        if (homeId) {
            $.ajax({
                url: '/Usages/GetAppliancesByHome',
                data: { homeId: homeId },
                success: function (data) {
                    $.each(data, function (index, appliance) {
                        applianceDropdown.append('<option value="' + appliance.applianceId + '">' + appliance.name + '</option>');
                    });
                },
                error: function () {
                    alert("Error fetching appliances.");
                }
            });
        }
    });
});
