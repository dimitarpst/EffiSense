$(document).ready(function () {

    $('#sidebarToggle').on('click', function () {
        const $sidebar = $('#sidebar');
        $sidebar.toggleClass('collapsed');
        $('#content-wrapper').toggleClass('collapsed');

        $('.navbar').toggleClass('collapsed-navbar');


        if ($(window).width() < 992) {
            $('body').toggleClass('modal-open');
        }
    });



    $(document).click(function (e) {
        if (
            !$(e.target).closest('#sidebar, #sidebarToggle').length &&
            $('#sidebar').hasClass('collapsed') &&
            $(window).width() < 992
        ) {
            $('#sidebar').removeClass('collapsed');
            $('.navbar').removeClass('collapsed-navbar');
            $('body').removeClass('modal-open');
        }
    });
});

