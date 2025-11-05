//const feedbackModal = document.getElementById('feedbackModal');
//const sendFeedbackBtn = document.getElementById('sendFeedbackBtn');
//const closeButton = document.querySelector('.close-button');
//const feedbackForm = document.getElementById('feedbackForm');

    // When the user clicks the button, open the modal 
    function displayForm () {
    feedbackModal.style.display = 'flex'; // Use flex to center
}

//// When the user clicks on <span> (x), close the modal
//closeButton.onclick = function () {
//    feedbackModal.style.display = 'none';
//}

//// When the user clicks anywhere outside of the modal, close it
//window.onclick = function (event) {
//    if (event.target == feedbackModal) {
//        feedbackModal.style.display = 'none';
//    }
//}

$(document).ready(function () {

    $('.feedback-button').on('click', function () {
        $('.modal').show();
        $('.modal').addClass('flex');
    });

    $('.close-button').on('click', function () {
        $('.modal').hide();
        $('.modal').removeClass('flex');
    });


    $('.has-dropdown').on('click', function () {

        if ($(this).hasClass('active')) {
            $(this).removeClass('active');
        }
        else {
            $(this).addClass("active");
        }
        
    });

    $('.searchInCategory').on('click', function () {

        currentQuery = $('#searchQuery').val().trim();
        currentCategory = $('#searchCategory').val().trim();
        const matchType = $('input[name="matchType"]:checked').val();
        const searchIn = $('input[name="searchIn"]:checked').val();

    });

});

$(document).ready(function () {
    $('.has-dropdown .main-item-trigger').on('click', function (e) {
        e.stopPropagation();
        $(this).closest('.has-dropdown').toggleClass('open');
        $(this).closest('.has-dropdown').toggleClass('active');
    });

    $(document).on('click', function () {
        $('.has-dropdown').removeClass('open');
    });
});
