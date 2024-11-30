$(document).ready(function () {
    const applianceDropdown = $("#ApplianceId");
    const selectedApplianceId = applianceDropdown.data("selected-id");

    $("#HomeId").change(function () {
        const homeId = $(this).val();

        applianceDropdown.empty().append('<option value="">Select Appliance</option>');

        if (homeId) {
            $.ajax({
                url: '/Usages/GetAppliancesByHome',
                data: { homeId: homeId },
                success: function (data) {
                    $.each(data, function (index, appliance) {
                        const selected = appliance.applianceId == selectedApplianceId ? 'selected' : '';
                        applianceDropdown.append(`<option value="${appliance.applianceId}" ${selected}>${appliance.name}</option>`);
                    });
                },
                error: function () {
                    alert("Error fetching appliances.");
                }
            });
        }
    });

    const selectedHomeId = $("#HomeId").val();
    if (selectedHomeId) {
        $("#HomeId").trigger("change");
    }
});
