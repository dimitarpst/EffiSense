$(document).ready(function () {
    const applianceDropdown = $("#ApplianceId");
    const homeDropdown = $("#HomeId");

    if (!applianceDropdown.length || !homeDropdown.length) {
        console.error("Required dropdowns not found in DOM.");
        return;
    }

    const selectedApplianceId = applianceDropdown.data("selected-id");

    homeDropdown.change(function () {
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

    const selectedHomeId = homeDropdown.val();
    if (selectedHomeId) {
        homeDropdown.trigger("change");
    }
});
