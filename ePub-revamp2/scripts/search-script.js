$(document).ready(function () {

    $('.most-searched-dropdown').hide();
    $('.sort-container').hide();

    //search whole
    $('.search-container').on('submit', function (e) {
        e.preventDefault();
        currentQuery = $('#searchQuery').val().trim();
        matchType =$('input[name="matchType"]:checked').val();
        searchIn = $('input[name="searchIn"]:checked').val();
        currentCulture = $('#currentCulture').val();
        $('.search-container').removeClass('focus');

        $('.most-searched-dropdown').hide();

        console.log(searchIn);
        console.log(matchType);


        $('.content-area').hide();
        $('.search-result-area').removeClass('hide');

        if (!currentQuery) {
                    alert('Please enter a search term.');
                    return;
        }

        if (currentQuery) {
            currentPage = 1;
            matchType = $('input[name="matchType"]:checked').val();

            if (matchType == "senmatic") {
                performSearch(currentPage, sortBy);
            }
            else {
                performSearchNormal(currentPage, sortBy);
            }
        }
    });

    $('#searchBtn').on('click', function () {
        $('.search-container').trigger('submit');
    });

    //search in Category
    $('#searchBtnCategory').on('click', function () {

        currentQuery = $('#searchQuery').val().trim();
        currentCategory = $('#searchCategory').val().trim();
        currentCulture = $('#currentCulture').val();
        matchType = $('input[name="matchType"]:checked').val();
        searchIn = $('input[name="searchIn"]:checked').val();
        $('.most-searched-dropdown').hide();

        currentId = document.getElementById("currentId").textContent;

        $('.main-content').hide();


        if (!currentQuery) {
            alert('Please enter a search term.');
            return;
        }

        if (currentQuery) {
            performSearch(currentQuery,currentId)
        }


    });

    //search help page
    $('#searchBtnHelp').on('click', function () {

        currentQuery = $('#searchQuery').val().trim();


        if (!currentQuery) {
            alert('Please enter a search term.');
            return;
        }

        if (currentQuery) {
            performSearch(currentQuery, currentId)
        }


    });


    $(document).on('click', '.most-searched-item', function () {
        var term = this.textContent.trim();

        var checkTerm = this;

        if (checkTerm.classList.contains("item-manuals")) {
            term = $(".search-title").textContent;
            $('#searchOverlay').hide();
            $('.search-container').removeClass('focus');
            $('.most-searched-dropdown').hide();
            $('.ghost-container').hide();
        }

        else {
            $('#searchQuery').val(term); // set input value
            $('#searchQuery').focus();
            $('.recent-searches').hide();
        }
    });


    //document.getElementById("home").addEventListener("submit", function (e) {
    //    e.preventDefault(); // stop actual form submit/reload

    //    // your script here
    //    alert("Form submitted via Enter or button!");
    //});


    $(document).on('click', '.page-btn', function () {
        currentPage = parseInt($(this).data('page'));
        performSearch(currentPage);
    });

});

// new


let currentPage = 1;
let currentQuery = "";
let matchType = "";
let searchIn = "";
let currentCategory = "";
let sortBy = "";
let currentCulture = "";
let currentId = "";

function performSearch(page = 1) {

    Swal.fire({
        title: 'Searching...',
        text: 'Please wait while we find the best results for you.',
        allowOutsideClick: false,
        didOpen: () => {
            Swal.showLoading();
        }
    });


        $.ajax({
            url: '/umbraco/api/SemanticSearch/Search',
            data: { query: currentQuery, limit: 10, page: 1, sortBy: sortBy },
            method: 'GET',
            success: function (data) {
                // Clear previous results
                $('.search-container').removeClass('focus');
                $('.search-overlay').hide();
                Swal.close();
                console.log(data);

                $('#searchResults').empty();
                const $pagination = $('#pagination');
                $pagination.empty();
                $('.sort-container').show();
                $('.filter-sort-options').removeClass("hide");

               

                //if (results.length === 0) {
                //    $('#searchResults').append('<p>No results found.</p>');
                //    return;
                //}

                if (data.results != null) {

                    if (data.results.length > 0) {

                        var resultHtml = '';


                        if (data.results != "") {
                            /*resultHtml += '<p>Did you mean <b>' + data.query + '</b>?</p>';*/

                            resultHtml += '<p>Found ' + data.results.length + ' result(s) for <b>"' + data.query + '"<b></p>';
                        }
                        else {
                            resultHtml += '<p>Found ' + data.results.length + ' result(s) for <b>"' + data.query + '"<b></p>';
                        }

                        console.log(data.aiAnswer);

                        resultHtml += '<div class=aiAnswerBox><b style="font-weight:300">Loading reasoning...</b></div>';


                        // Loop through results and display
                        data.results.forEach(function (item) {
                            console.log(item);
                            /*const resultHtml = */

                            resultHtml += '<div class="result-item">';
                            resultHtml += '<h4><a href="' + item.directedUrl + '">' + item.Name + '</a></h4><div class="item-link" style="margin:5px 0 10px 0; color:grey;"><p style="margin:0; font-size:0.9em;">' + item.ParentName + ' </p><p style="margin:0; font-size:0.9em;">' + item.Url + ' </p></div><span class="description" style="font-weight:400; font-size:15px;">' + item.Snippet + '</span> <p class="meta-info" style="padding-top:20px;"><span>Published Date: ' + item.publishedDate + '</span></p></div>';



                        });
                        $('#searchResults').append(resultHtml);

                        var topResult = data.results;
                        var $topDiv = $('#searchResults .result-item').first();

                        // Add loading spinner div
                        //$topDiv.append('<div class="reasoning-box" style="margin-top:10px; padding:10px; background:#f9f9f9; border-left:4px solid #4CAF50;">Loading reasoning...</div>');

                        // Fetch reasoning
                        $.ajax({
                            url: '/umbraco/api/SemanticSearch/GetReasoning',
                            method: 'POST',
                            contentType: 'application/json',
                            data: JSON.stringify({
                                Query: data.query,
                                Candidates: topResult
                            }),
                            success: function (reasoningData) {
                                if (reasoningData && reasoningData.length > 0) {
                                    var reasoningText = reasoningData[0].Reasoning || "Reasoning unavailable";
                                    $('.aiAnswerBox').html('');
                                    $('.aiAnswerBox').html('<div class="aiBoxTitle" style="display:flex;flex-wrap:nowrap;align-items:center;"><i class="fas fa-brain" style="color:#d3e3fd;"></i><div class="titleAi" style="font-size: 20px;font-weight:600;margin: 5px;">AI Reasoning </div></div> <strong>Reasoning:</strong> ' + reasoningText);
                                }
                            },
                            error: function (xhr, status, error) {
                                $('.aiAnswerBox').html('<strong>Reasoning:</strong> Unavailable');
                                console.error("Reasoning error:", error);
                            }
                        });

                    } else {
                        $('#searchResults').append('<p>No results found.</p>');
                    }
                }
            },
            error: function (xhr, status, error) {
                console.error('Search error:', error);
                $('#searchResults').html('<p>Sorry, an error occurred during search.</p>');
            }
        });







    



       // old search
    //$.ajax({
    //    url: '/umbraco/surface/searchsurface/ajaxsearch',
    //    type: 'GET',
    //    data: {
    //        query: currentQuery,
    //        matchType: matchType,
    //        searchIn: searchIn,
    //        currentCulture: currentCulture,
    //        sortBy: sortBy,
    //        page: page,
    //        currentCategory: currentCategory
            
    //    },
    //    success: function (data) {
    //        var results = $('.search-result');

    //        results.empty();
    //        const $pagination = $('#pagination');
    //        $pagination.empty();
    //        $('.sort-container').show();
    //        $('.filter-sort-options').removeClass("hide");

    //        console.log(data);

    //        if (data.enrichedResults != null) {

    //            if (data.enrichedResults.length > 0) {

    //                var html = '';

    //                console.log(data);


    //                if (data.finalSearchTerm != "") {
    //                    html += '<p>Did you mean <b>' + data.finalSearchTerm + '</b>?</p>';

    //                    html += '<p>Found ' + data.enrichedResults.length + ' result(s) for <b>"' + data.finalSearchTerm + '"<b></p>';
    //                }
    //                else {
    //                    html += '<p>Found ' + data.enrichedResults.length + ' result(s) for <b>"' + currentQuery + '"<b></p>';
    //                }
    //                $.each(data.enrichedResults, function (i, item) {
    //                    console.log(data);
    //                    console.log(item);

    //                    var description = item.bodyContent;

    //                    //if (!description.includes('<mark')) {
    //                    //    description = "";
    //                    //}

    //                    html += '<div class="result-item">';
    //                    html += '<h4><a href="' + item.url + '">' + item.title + '</a></h4><div class="item-link" style="margin:5px 0 10px 0; color:grey;"><p style="margin:0; font-size:0.9em;">' + item.manualName + ' </p><p style="margin:0; font-size:0.9em;">' + item.url + ' </p></div><span class="description" style="font-weight:400; font-size:15px;">' + description + '</span> <p class="meta-info" style="padding-top:20px;"><span>Published Date: ' + item.updateDate + '</span></p></div>';
    //                });
    //                //html += '<div class="result-item"';
    //                //html += `<p>Page ${data.currentPage} of ${data.totalPages}</p>`;
    //                results.html(html);



    //                const cp = data.currentPage;
    //                const tp = data.totalPages;

    //                // « Prev
    //                $pagination.append(`<button class="page-btn" data-page="${cp - 1}" ${cp === 1 ? 'disabled' : ''}>«</button>`);


    //                // Add pagination buttons
    //                //if (data.totalPages > 1) {
    //                //    if (data.currentPage > 1) {
    //                //        $pagination.append(`<button class="page-btn" data-page="${data.currentPage - 1}">Previous</button>`);
    //                //    }
    //                //    if (data.currentPage < data.totalPages) {
    //                //        $pagination.append(`<button class="page-btn" data-page="${data.currentPage + 1}">Next</button>`);
    //                //    }
    //                //}


    //                for (let i = 1; i <= tp; i++) {
    //                    const btnClass = (i === cp) ? 'active-page' : '';
    //                    $pagination.append(`<button class="page-btn ${btnClass}" data-page="${i}" ${i === cp ? 'disabled' : ''}>${i}</button>`);
    //                }

    //                // » Next
    //                $pagination.append(`<button class="page-btn" data-page="${cp + 1}" ${cp === tp ? 'disabled' : ''}>»</button>`);
    //            } else {
    //                results.html('<p>No results found.</p>');
    //            }
    //        }
    //        else {
    //            results.html('<p>No results found.</p>');
    //        }
    //    },
    //    error: function () {
    //        $('#searchResults').html('<p>Search failed.</p>');
    //    }
    //});

}



function performSearchNormal(page = 1) {

    Swal.fire({
        title: 'Searching...',
        text: 'Please wait while we find the best results for you.',
        allowOutsideClick: false,
        didOpen: () => {
            Swal.showLoading();
        }
    });


    $.ajax({
        url: '/umbraco/surface/searchsurface/AjaxSearch2',
        type: 'GET',
        data: {
            query: currentQuery,
            matchType: matchType,
            searchIn: searchIn,
            currentCulture: currentCulture,
            sortBy: sortBy,
            page: page,
            currentCategory: currentCategory

        },
        success: function (data) {
            var results = $('.search-result');
            Swal.close();
            results.empty();
            const $pagination = $('#pagination');
            $pagination.empty();
            $('.sort-container').show();
            $('.filter-sort-options').removeClass("hide");

            console.log(data);
            $('.search-container').removeClass('focus');
            $('.search-overlay').hide();
            if (data.results != null) {

                if (data.results.length > 0) {

                    var html = '';

                    console.log(data);


                    if (data.searchSelection == "fuzzy") {
                        html += '<p>Did you mean <b>' + data.didYouMean + '</b>?</p>';

                        html += '<p>Found ' + data.totalResults + ' result(s) for <b>"' + data.didYouMean + '"<b></p>';
                    }
                    else {
                        html += '<p>Found ' + data.totalResults + ' result(s) for <b>"' + currentQuery + '"<b></p>';
                    }



                    $.each(data.results, function (i, item) {
                        console.log(data);
                        console.log(item);

                        var description = item.bodyContent;

                        //if (!description.includes('<mark')) {
                        //    description = "";
                        //}

                        html += '<div class="result-item">';
                        html += '<h4><a href="' + item.directedUrl + '">' + item.title + '</a></h4><div class="item-link" style="margin:5px 0 10px 0; color:grey;"><p style="margin:0; font-size:0.9em;">' + item.manualName + ' </p><p style="margin:0; font-size:0.9em;">' + item.url + ' </p></div><span class="description" style="font-weight:400; font-size:15px;">' + description + '</span> <p class="meta-info" style="padding-top:20px;"><span>Published Date: ' + item.updateDate + '</span></p></div>';
                    });
                    //html += '<div class="result-item"';
                    //html += `<p>Page ${data.currentPage} of ${data.totalPages}</p>`;
                    results.html(html);



                    const cp = data.currentPage;
                    const tp = data.totalPages;

                    // « Prev
                    $pagination.append(`<button class="page-btn" data-page="${cp - 1}" ${cp === 1 ? 'disabled' : ''}>«</button>`);


                    // Add pagination buttons
                    //if (data.totalPages > 1) {
                    //    if (data.currentPage > 1) {
                    //        $pagination.append(`<button class="page-btn" data-page="${data.currentPage - 1}">Previous</button>`);
                    //    }
                    //    if (data.currentPage < data.totalPages) {
                    //        $pagination.append(`<button class="page-btn" data-page="${data.currentPage + 1}">Next</button>`);
                    //    }
                    //}


                    for (let i = 1; i <= tp; i++) {
                        const btnClass = (i === cp) ? 'active-page' : '';
                        $pagination.append(`<button class="page-btn ${btnClass}" data-page="${i}" ${i === cp ? 'disabled' : ''}>${i}</button>`);
                    }

                    // » Next
                    $pagination.append(`<button class="page-btn" data-page="${cp + 1}" ${cp === tp ? 'disabled' : ''}>»</button>`);
                } else {
                    results.html('<p>No results found.</p>');
                }
            }
            else {
                results.html('<p>No results found.</p>');
            }
        },
        error: function () {
            $('#searchResults').html('<p>Search failed.</p>');
        }
    });
}




//('.most-searched-item').on('click', () => {
//    document.getElementById('searchInput').value = term;

//    // Optional: trigger search automatically
//    document.getElementById('searchForm').submit();
//});


//search recomendation
document.addEventListener("DOMContentLoaded", function () {

    $('.recent-searches-box').hide();

const input = document.getElementById("searchQuery");
const suggestionBox = document.getElementById("autocompleteList");
    let debounce;
    let currentId = "";
    let helpId = "";


    input.addEventListener("keyup", function () {
        clearTimeout(debounce);
        const query = this.value.trim();
        var currentId = "";
        /*var currentCulture = "";*/
        currentCulture = document.getElementById("currentCultureSpan").textContent;

        var element = document.getElementById("currentId");
        if (element != null) {
            currentId = document.getElementById("currentId").textContent;
        } 

        var element2 = document.getElementById("help-id");
        if (element2 != null) {
            helpId = document.getElementById("help-id").textContent;
        }
        
       
        if (query.length < 2) {
            $('.recomend-list').empty();
            $('.recomend-list').hide();
            /* $('.search-container').removeClass("focus");*/
            $('.recent-searches-box').show();
            return;
        }


        if (currentId != "") {
            $.ajax({
                url: '/umbraco/surface/searchsurface/searchRecommendationManual',
                method: 'GET',
                data: {
                    query: query,
                    currentCulture: currentCulture,
                    currentId: currentId
                },
                success: function (data) {

                    if (data.length > 0) {

                        $('.recomend-list').show();
                        var searchContainer = $('.search-container');
                        var displayrecomendation = $('.recomend-list');
                        displayrecomendation.empty();
                        var html = '';
                        /* html += ' <h5>Most Searched</h5><div id="mostSearchedItems">';*/

                        searchContainer.addClass("focus");

                        for (let i = 0; i < data.length; i++) {
                            console.log(data[i].bodyContent);
                            html += ` <a class="most-searched-item item-manuals" style="text-decoration:none;" href="#${data[i].title}">${data[i].title}<div class="search-title" hidden>${data[i].title}</div></div><p style="margin-top:0px;margin-bottom:20px; padding-left:8px;">${data[i].bodyContent}</p>`;
                        }
                        html += '</div>';

                        displayrecomendation.html(html);

                        //console.log('Suggestions:', data);
                        //Render suggestion list
                    }
                },
                error: function (xhr, status, error) {
                    console.error('Error:', error);
                }
            });
        }

        else if (helpId != "") {
            $.ajax({
                url: '/umbraco/surface/searchsurface/searchRecommendationHelp',
                method: 'GET',
                data: {
                    query: query,
                    helpId: helpId
                },
                success: function (data) {

                    if (data.length > 0) {

                        $('.recomend-list').show();
                        var searchContainer = $('.search-container');
                        var displayrecomendation = $('.recomend-list');
                        displayrecomendation.empty();
                        var html = '';
                        /* html += ' <h5>Most Searched</h5><div id="mostSearchedItems">';*/

                        searchContainer.addClass("focus");

                        for (let i = 0; i < data.length; i++) {
                            console.log(data[i].bodyContent);
                            html += ` <a href="#${data[i].title}" class="most-searched-item item-manuals" id="manual-direct" onclick="getManualfromRecomended(this)" style="text-decoration:none;">${data[i].title}</div><div class="hidden-manual" hidden>${data[i].parents}</div><p style="margin-top:0px;margin-bottom:20px; padding-left:8px;">${data[i].bodyContent}</p>`;
                        }
                        html += '</div>';

                        displayrecomendation.html(html);

                        //console.log('Suggestions:', data);
                        //Render suggestion list
                    }
                },
                error: function (xhr, status, error) {
                    console.error('Error:', error);
                }
            });
        }




        else {
            $.ajax({
                url: '/umbraco/surface/searchsurface/searchRecommendation',
                method: 'GET',
                data: {
                    query: query,
                    currentCulture: currentCulture,
                    currentCategory: currentCategory
                },
                success: function (data) {

                    if (data.length > 0) {

                        $('.recomend-list').show();
                        var searchContainer = $('.search-container');
                        var displayrecomendation = $('.recomend-list');
                        displayrecomendation.empty();
                        var html = '';
                        /* html += ' <h5>Most Searched</h5><div id="mostSearchedItems">';*/

                        searchContainer.addClass("focus");

                        for (let i = 0; i < data.length; i++) {
                            html += ` <div class="most-searched-item">${data[i].title}</div>`;
                        }
                        html += '</div>';

                        displayrecomendation.html(html);

                        //console.log('Suggestions:', data);
                        //Render suggestion list
                    }
                },
                error: function (xhr, status, error) {
                    console.error('Error:', error);
                }
            });
        }
});
});


document.addEventListener("DOMContentLoaded", function () {
    const input = document.getElementById("searchQuery");
    const ghostPrefix = document.getElementById("ghostPrefix");
    const ghostSuffix = document.getElementById("ghostSuffix");
    const sortDropdown = document.getElementById("sortBy");
    currentCulture = document.getElementById("currentCultureSpan").textContent;

    let debounce;
    let firstSuggestion = "";

    input.addEventListener("keyup", function () {
        clearTimeout(debounce);
        const query = this.value;
        $('.ghost-container').show();

        $('.recent-searches-box').hide();

        if (query.length < 2) {
            ghostPrefix.textContent = "";
            ghostSuffix.textContent = "";
            return;
        }

        debounce = setTimeout(() => {
            $.ajax({
                url: '/umbraco/surface/searchsurface/searchRecommendation',
                method: 'GET',
                data: { query: query, currentCulture: currentCulture, currentCategory: currentCategory },
                success: function (data) {
                    if (data.length > 0) {
                        firstSuggestion = data[0].title;

                        if (firstSuggestion.toLowerCase().startsWith(query.toLowerCase())) {
                            ghostPrefix.textContent = query;
                            ghostSuffix.textContent = firstSuggestion.substring(query.length);
                        } else {
                            ghostPrefix.textContent = "";
                            ghostSuffix.textContent = "";
                        }
                    } else {
                        ghostPrefix.textContent = "";
                        ghostSuffix.textContent = "";
                    }
                }
            });
        }, 0);
    });

    input.addEventListener("keydown", function (e) {
        if ((e.key === "Tab" || e.key === "ArrowRight") && ghostSuffix.textContent !== "") {
            e.preventDefault();
            input.value = ghostPrefix.textContent + ghostSuffix.textContent;
            ghostPrefix.textContent = "";
            ghostSuffix.textContent = "";
        }
    });

    if (sortDropdown != null) {
        sortDropdown.addEventListener("change", function () {
            sortBy = $('#sortBy').val().trim();
            performSearch(1); // Reset to page 1 on sort
        });
    }




    //$('.item-manuals').on('click', function (e) {
    //    e.preventDefault();

    //    alert("in");
    //    ('.focus').removeClass('focus');
    //});


    //$('a .most-searched-item').click(function (e) {
    //    if (!$(this).hasClass('unclickable')) {
    //        // allow normal redirect
    //    } else {
    //        e.preventDefault();
    //        alert("in");
    //    }
    //});





});

$(document).ready(function () {
    const $overlay = $('#searchOverlay');
    const $searchInput = $('#searchQuery');

    // Show overlay on focus
    $searchInput.on('focus', function () {
        $overlay.show();
        $('.ghost-container').show();
        $('.most-searched-dropdown').show();
        $('.recomend-list').hide();
        $('.search-container').addClass("focus");

        if (currentQuery != null) {
            $('.ghost-container').hide();
        }
       

        if (currentQuery == "") {
            $('.recent-searches-box').show();

        }
    });

    // Hide overlay when clicking outside the search box
    $(document).on('click', function (e) {
        if (!$(e.target).closest('.search-container').length) {
            $overlay.hide();
            $('.ghost-container').hide();

            
            $('.most-searched-dropdown').hide();
            $('.search-container').removeClass("focus");
            $('.filter-dropdown').hide();
                     
        }
    });

    // When the user clicks on the button, scroll to the top of the document
    function topFunction() {
        document.body.scrollTop = 0;
        document.documentElement.scrollTop = 0;
    }




});



document.addEventListener("DOMContentLoaded", function () {
    const links = document.querySelectorAll("a.most-searched-item");

    links.forEach(function (link) {
        link.addEventListener("click", function () {
            alert("in");
        });
    });
});


