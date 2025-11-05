$(document).ready(function ready() {
    $('.filter-dropdown2').click(handleDropDownClick);

});

function handleDropDownClick() {
    var target = $(this);
    var dropDownList = $('.filter-dropdown');
    var offset = target.offset();
    if (dropDownList.hasClass('show')) {
        dropDownList.hide()
        dropDownList.removeClass('show')
    } else {
        dropDownList.addClass('show')
        dropDownList.show()
        }
}


////display recen opened manuals
$(function () {

    currentCulture = document.getElementById("currentCultureSpan").textContent;

    if (currentCulture == "pbb") {

        const recent = JSON.parse(localStorage.getItem('recentManuals') || '[]');

        if (recent.length === 0) {
            $('.doc-list-recent').html("<p class='text-muted'>No recently viewed manuals.</p>");
        } else {
            let html = '';
            recent.forEach(manual => {
                html += `<a href="${manual.url}" class="doc-item" style="text-decoration:none; cursor:pointer;" data-title="${manual.title}">
                <div class="doc-title" style="text-decoration:none; color:rgb(51, 51, 51);">${manual.title}</div>
                <div class="doc-date">${manual.date}</div>
                </a>`;
            });
            /*html += '</div>';*/

            $('.doc-list-recent').append(html);
        }

    }

    else if (currentCulture == "pibb") {

        const recent = JSON.parse(localStorage.getItem('recentManualsPibb') || '[]');

        if (recent.length === 0) {
            $('.doc-list-recent').html("<p class='text-muted'>No recently viewed manuals.</p>");
        } else {
            let html = '';
            recent.forEach(manual => {
                html += `<a href="${manual.url}" class="doc-item" style="text-decoration:none; cursor:pointer;" data-title="${manual.title}">
                <div class="doc-title" style="text-decoration:none; color:rgb(51, 51, 51);">${manual.title}</div>
                <div class="doc-date">${manual.date}</div>
                </a>`;
            });
            /*html += '</div>';*/

            $('.doc-list-recent').append(html);
        }

    }




});

//display all manuals in pbb
//function loadManualsUnder1207() {
//    currentCulture = document.getElementById("currentCultureSpan").textContent;

//    $.get('/umbraco/api/manuallist/getmanualsunder1207?', { currentCulture: currentCulture }, function (data) {

//        const count = data.total;
//        $('.doc-count').text(`Total Manuals: ${count}`);

//        data.items.forEach(doc => {
//                const html = `
//                <a href="${doc.url}" class="doc-item" style="text-decoration:none; cursor:pointer;" data-title="${doc.name}">
//                <div class="doc-title" style="text-decoration:none; color:rgb(51, 51, 51);">${doc.name}</div>
//                <div class="doc-date">${doc.date}</div>
//                </a>`;
//                $('.doc-list-all').append(html);
//            });
//        });
//}



//old load manual in manuals page / folderList page
//function loadManualsUnder1207() {
//    const currentCulture = document.getElementById("currentCultureSpan").textContent;

//    $.get('/umbraco/api/manuallist/getmanualsunder1207', { currentCulture: currentCulture }, function (data) {
//        const count = data.total;
//        $('.doc-count').text(`Total documents: ${count}`);

//        data.items.forEach(doc => {
//            const hasChildren = doc.children && doc.children.length > 0;

//            // Parent manual
//            const parentHtml = `
//                <div class="doc-item-wrapper">
//                    <div class="doc-item" style="display: flex; justify-content: space-between; align-items: center; padding:22px 26px; font-weight:500;" data-date="${doc.date}">
                        
//                        <!-- Title (clickable link) -->
//                        <a href="${doc.url}" style="flex: 1; color:rgb(51, 51, 51); font-weight: 500; text-decoration: none;">
//                            ${doc.name}
//                        </a>

//                         <!-- url Name -->
//                        <div style="width: 150px; text-align: center; font-size: 14px; color: #888;">
//                            ${doc.urlName}
//                        </div>

//                        <!-- Date -->
//                        <div style="width: 150px; text-align: right; font-size: 14px; color: #888;">
//                            ${doc.date}
//                        </div>

//                        <!-- Expand Manual -->
//                        ${hasChildren ? `
//                        <div class="expand-toggle" style="width: 150px; text-align: right; display: flex; justify-content: flex-end; align-items: center; gap: 5px; cursor: pointer;">
//                            <span class="expand-label" style="font-size: 14px; color:#007bff;">Expand Manual</span>
//                            <span class="arrow-icon" style="font-size: 14px;">▼</span>
//                        </div>` : '<div style="width: 150px;"></div>'}
//                    </div>

//                    ${hasChildren ? `
//                        <div class="doc-children" style="display:none; padding-left: 20px;">
//                            ${doc.children.map(child => `
//                                <a href="${child.url}" class="doc-item"
//                                    style="text-decoration:none; display: flex; justify-content: space-between; align-items: center; padding: 22px 26px;">
                                    
//                                    <!-- Child title -->
//                                    <div style="flex: 1; color:rgb(51, 51, 51);">
//                                        ${child.name}
//                                    </div>

//                                    <!-- Child date -->
//                                    <div style="width: 150px; text-align: right; font-size: 13px; color: #666;">
//                                        ${child.date}
//                                    </div>

//                                    <div style="width: 150px;"></div>
//                                </a>
//                            `).join('')}
//                        </div>
//                    ` : ''}
//                </div>
//            `;

//            $('.doc-list-all').append(parentHtml);
//        });

//        // Toggle only when clicking the expand label or arrow
//        $('.doc-list-all').on('click', '.expand-toggle', function (e) {
//            e.stopPropagation();
//            const $parentWrapper = $(this).closest('.doc-item-wrapper');
//            const $children = $parentWrapper.find('.doc-children');
//            const $arrow = $(this).find('.arrow-icon');

//            $children.slideToggle(200);
//            $arrow.text($arrow.text() === '▼' ? '▲' : '▼');
//        });
//    });
//}




let manualsData = []; // Store API results

function loadManualsUnder1207() {
    const currentCulture = document.getElementById("currentCultureSpan").textContent;

    $.get('/umbraco/api/manuallist/getmanualsunder1207', { currentCulture: currentCulture }, function (data) {
        manualsData = data.items; // Store for sorting
        renderManuals(manualsData);
        renderAlphabetFilter(manualsData);

        // Show total count
        $('.doc-count').text(`Total documents: ${data.total}`);

       


    });
}

function renderManuals(data) {
    const $list = $('.doc-list-all');
    $list.empty();

    data.forEach(doc => {
        const hasChildren = doc.children && doc.children.length > 0;

        const parentHtml = `
            <div class="doc-item-wrapper">
                <div class="doc-item" style="display: flex; justify-content: space-between; align-items: center; padding:22px 26px; font-weight:500;" data-date="${doc.date}">
                    <a href="${doc.url}" style="flex: 1; color:rgb(51, 51, 51); font-weight: 500; text-decoration: none;">
                        ${doc.name}
                    </a>
                    <div style="width: 150px; text-align: center; font-size: 14px; color: #888;">
                        ${doc.urlName}
                    </div>
                    <div style="width: 150px; text-align: right; font-size: 14px; color: #888;">
                        ${doc.date}
                    </div>
                    ${hasChildren ? `
                    <div class="expand-toggle" style="width: 150px; text-align: right; display: flex; justify-content: flex-end; align-items: center; gap: 5px; cursor: pointer;">
                        <span class="expand-label" style="font-size: 14px; color:#007bff;">Expand Manual</span>
                        <span class="arrow-icon" style="font-size: 14px;">▼</span>
                    </div>` : '<div style="width: 150px;"></div>'}
                </div>
                ${hasChildren ? `
                    <div class="doc-children" style="display:none; padding-left: 20px;">
                        ${doc.children.map(child => `
                            <a href="${child.url}" class="doc-item"
                                style="text-decoration:none; display: flex; justify-content: space-between; align-items: center; padding: 22px 36px;">
                                <div style="flex: 1; color:rgb(51, 51, 51);">
                                    ${child.name}
                                </div>
                                <div style="width: 150px; text-align: right; font-size: 13px; color: #666;">
                                    ${child.date}
                                </div>
                                <div style="width: 150px;"></div>
                            </a>
                        `).join('')}
                    </div>
                ` : ''}
            </div>
        `;

        $list.append(parentHtml);
    });

    // Toggle expand/collapse
    $('.doc-list-all').off('click', '.expand-toggle').on('click', '.expand-toggle', function (e) {
        e.stopPropagation();
        const $parentWrapper = $(this).closest('.doc-item-wrapper');
        const $children = $parentWrapper.find('.doc-children');
        const $arrow = $(this).find('.arrow-icon');

        $children.slideToggle(200);
        $arrow.text($arrow.text() === '▼' ? '▲' : '▼');
    });
}

$(document).ready(function () {
    // Load manuals initially
    loadManualsUnder1207();

    // Bind sort event
    $('#sortBy').on('change', function () {
        const sortOption = $(this).val();
        sortManuals(sortOption);
    });


    // Alphabet filter event
    $('.alphabet-filter').on('click', '.letter', function () {
        const letter = $(this).data('letter');
        filterManualsByLetter(letter);
    });






    $('.doc-header [data-sort-key]').on('click', function () {
        const $header = $(this);
        const sortKey = $header.data('sort-key');
        let sortOrder = $header.data('sort-order');

        // Toggle sort order
        sortOrder = sortOrder === 'asc' ? 'desc' : 'asc';
        $header.data('sort-order', sortOrder);

        // Reset other headers
        $('.doc-header [data-sort-key]').not($header).data('sort-order', 'asc')
            .find('.sort-arrow')
            .removeClass('fa-sort-up fa-sort-down')
            .addClass('fa-sort');

        // Update active header icon
        const $icon = $header.find('.sort-arrow');
        $icon.removeClass('fa-sort fa-sort-up fa-sort-down');
        $icon.addClass(sortOrder === 'asc' ? 'fa-sort-up' : 'fa-sort-down');

        // Call sorting
        sortManuals(sortKey, sortOrder);
    });




});



function renderAlphabetFilter(manuals) {
    const alphabetContainer = document.getElementById('alphabetNav');

    if (alphabetContainer != null) {
        alphabetContainer.innerHTML = ''; // clear existing letters

        // Always show "All" first
        const allBtn = document.createElement('button');
        allBtn.textContent = 'All';
        allBtn.classList.add('alphabet-btn');
        allBtn.addEventListener('click', () => renderManuals(manuals));
        alphabetContainer.appendChild(allBtn);

        // Track which letters have manuals
        const availableLetters = new Set();

        manuals.forEach(manual => {
            const firstLetter = manual.name.charAt(0).toUpperCase();
            if (firstLetter.match(/[A-Z]/)) {
                availableLetters.add(firstLetter);
            }
        });

        // Create buttons only for available letters
        [...availableLetters].sort().forEach(letter => {
            const btn = document.createElement('button');
            btn.textContent = letter;
            btn.classList.add('alphabet-btn');
            btn.addEventListener('click', () => {
                const filtered = manuals.filter(m =>
                    m.name.toUpperCase().startsWith(letter)
                );
                renderManuals(filtered);
            });
            alphabetContainer.appendChild(btn);
        });
    }
}




function filterManualsByLetter(letter) {
    $('.doc-item-wrapper').each(function () {
        const name = $(this).find('a').first().text().trim().toUpperCase();
        if (name.startsWith(letter)) {
            $(this).show();
        } else {
            $(this).hide();
        }
    });
}










function sortManuals(sortKey, sortOrder = 'asc') {
    const $manuals = $('.doc-list-all .doc-item-wrapper').get();

    $manuals.sort(function (a, b) {
        let valA, valB;

        if (sortKey === 'title') {
            valA = $(a).find('a').first().text().toLowerCase();
            valB = $(b).find('a').first().text().toLowerCase();
        }
        else if (sortKey === 'urlName') {
            valA = $(a).find('.doc-item > div').eq(1).text().toLowerCase(); // urlName div is 2nd div inside .doc-item
            valB = $(b).find('.doc-item > div').eq(1).text().toLowerCase();
        }
        else if (sortKey === 'date') {
            valA = new Date($(a).find('.doc-item').first().data('date'));
            valB = new Date($(b).find('.doc-item').first().data('date'));
        }
        else {
            valA = '';
            valB = '';
        }

        console.log('Comparing:', valA, valB);

        if (valA < valB) return sortOrder === 'asc' ? -1 : 1;
        if (valA > valB) return sortOrder === 'asc' ? 1 : -1;
        return 0;
    });

    $('.doc-list-all').empty().append($manuals);
}




function filterManualsByLetter(letter) {
    $('.doc-list-all .doc-item-wrapper').each(function () {
        const name = $(this).find('.doc-item a').first().text().trim();
        if (letter === 'all') {
            $(this).show();
        } else if (name.toUpperCase().startsWith(letter)) {
            $(this).show();
        } else {
            $(this).hide();
        }
    });
}





//change star toggle
$(document).on('click', '.star-toggle', function () {



    currentCulture = document.getElementById("currentCultureSpan").textContent;


    const $icon = $(this);
    $icon.toggleClass('far fas'); // Toggle between outline and solid
    $icon.css('color', $icon.hasClass('fas') ? 'gold' : '');

    const id = $icon.data('id');


    if (currentCulture == "pbb") {

        // Save to or remove from localStorage (optional)
        let starred = JSON.parse(localStorage.getItem('starredManuals') || '[]');
        if ($icon.hasClass('fas')) {
            if (!starred.includes(id)) starred.push(id);
        } else {
            starred = starred.filter(x => x !== id);
        }
        localStorage.setItem('starredManuals', JSON.stringify(starred));

    }


    else if (currentCulture == "pibb") {

        // Save to or remove from localStorage (optional)
        let starred = JSON.parse(localStorage.getItem('starredManualsPibb') || '[]');
        if ($icon.hasClass('fas')) {
            if (!starred.includes(id)) starred.push(id);
        } else {
            starred = starred.filter(x => x !== id);
        }
        localStorage.setItem('starredManualsPibb', JSON.stringify(starred));

    }





});

//save star manual in localStorage
$(document).ready(function () {

    currentCulture = document.getElementById("currentCultureSpan").textContent;


    if (currentCulture == "pbb") {

        // Get starred manual IDs from localStorage
        let starred = JSON.parse(localStorage.getItem('starredManuals') || '[]');

        // Loop through all star icons on the page
        $('.star-toggle').each(function () {
            const id = $(this).data('id');
            if (starred.includes(id)) {
                // If the ID is starred, fill the star
                $(this).removeClass('far').addClass('fas').css('color', 'gold');
            }
        });

    }

    else if (currentCulture == "pibb") {

        // Get starred manual IDs from localStorage
        let starred = JSON.parse(localStorage.getItem('starredManualsPibb') || '[]');

        // Loop through all star icons on the page
        $('.star-toggle').each(function () {
            const id = $(this).data('id');
            if (starred.includes(id)) {
                // If the ID is starred, fill the star
                $(this).removeClass('far').addClass('fas').css('color', 'gold');
            }
        });

    }



});




//const step = 3;
//let currentIndex = 0;

//function showManualItems() {
//    const items = $('.manuals-container .manual-item');
//    items.hide();
//    items.slice(0, currentIndex + step).fadeIn();
//    currentIndex += step;

//    if (currentIndex >= items.length) {
//        $('#showMoreBtn').hide();
//    } else {
//        $('#showMoreBtn').show();
//    }

//    if (currentIndex > step) {
//        $('#showLessBtn').show();
//    }
//}

//function showLessManualItems() {
//    const items = $('.manuals-container .manual-item');
//    currentIndex = step;
//    items.hide();
//    items.slice(0, step).fadeIn();
//    $('#showMoreBtn').show();
//    $('#showLessBtn').hide();
//}

//$(document).ready(function () {
//    $('.manuals-container .manual-item').hide();
//    showManualItems(); // Show first batch

//    $('body').on('click', '#showMoreBtn', function () {
//        showManualItems();
//    });

//    $('body').on('click', '#showLessBtn', function () {
//        showLessManualItems();
//    });
//});

$(document).ready(function () {
    const step = 4;

    $('.manuals-wrapper').each(function () {
        const $wrapper = $(this);
        const $items = $wrapper.find('.manual-item');
        const $showMore = $wrapper.find('.show-more-btn');
        const $showLess = $wrapper.find('.show-less-btn');
        let currentIndex = 0;

        function showItems() {
            $items.hide();
            $items.slice(0, currentIndex + step).fadeIn();
            currentIndex += step;

            if (currentIndex >= $items.length) {
                $showMore.hide();
            } else {
                $showMore.show();
            }

            if (currentIndex > step) {
                $showLess.show();
            }
        }

        function resetItems() {
            currentIndex = step;
            $items.hide();
            $items.slice(0, step).fadeIn();
            $showMore.show();
            $showLess.hide();
        }

        // Initial show
        $items.hide();
        showItems();

        $showMore.on('click', function () {
            showItems();
        });

        $showLess.on('click', function () {
            resetItems();
        });
    });




    $('.manuals-wrapper2').each(function () {
        const $wrapper = $(this);
        const $items = $wrapper.find('.manual-item');
        const $showMore = $wrapper.find('.show-more-btn');
        const $showLess = $wrapper.find('.show-less-btn');
        let currentIndex = 0;

        function showItems() {
            $items.hide();
            $items.slice(0, currentIndex + step).fadeIn();
            currentIndex += step;

            if (currentIndex >= $items.length) {
                $showMore.hide();
            } else {
                $showMore.show();
            }

            if (currentIndex > step) {
                $showLess.show();
            }
        }

        function resetItems() {
            currentIndex = step;
            $items.hide();
            $items.slice(0, step).fadeIn();
            $showMore.show();
            $showLess.hide();
        }

        // Initial show
        $items.hide();
        showItems();

        $showMore.on('click', function () {
            showItems();
        });

        $showLess.on('click', function () {
            resetItems();
        });
    });

});

function getManual(clickedElement) {

    var manualDiv = clickedElement.querySelector('#manual-direct');

    var titleText = manualDiv.textContent.trim();
    var titleClass = '.' + titleText;

   
    alert(titleClass);

    $(titleClass).show();

    $('.show').hide();

}

//$(document).ready(function () {
//    const container = $('.doc-list-star');
//    container.html('<p>Loading starred documents...</p>');

//    // Get list of starred IDs
//    const starred = JSON.parse(localStorage.getItem('starredManuals') || '[]');

//    if (starred.length === 0) {
//        container.html('<p>No starred documents.</p>');
//        return;
//    }

//    // Clear container for results
//    container.empty();


//    // Loop through each ID and call the API
//    starred.forEach(id => {
//        $.get(`/umbraco/api/starred/getmanualcontentbyid?id=${id}`, function (res) {
//            // Append result to container
//            container.append(`<a class="doc-item" style="text-decoration:none; cursor:pointer;" data-title="${res.name}">
//                <div class="doc-title" style="text-decoration:none; color:rgb(51, 51, 51);">${res.name}</div>
//                <div class="doc-date">${res.date}</div>
//            `);
//        }).fail(() => {
//            container.append(`<p style="color:red;">Failed to load document with ID ${id}</p>`);
//        });
//    });
//});


$(document).ready(function () {

$('#searchBtnManuals').on('click', function () {

    

    currentQuery = $('#searchQuery').val().trim();
    currentCulture = document.getElementById("currentCultureSpan").textContent;
    page = 1;

    $.ajax({
        url: '/umbraco/surface/searchsurface/searchinmanuals',
        type: 'GET',
        data: {
            query: currentQuery,
            currentCulture: currentCulture,
            page: page,

        },
        success: function (data) {
            var results = $('.doc-list-search');

            $('.show').addClass("hide");
            $('.show').removeClass("show");
            $('.active').removeClass("active");

            results.empty();
            const $pagination = $('#pagination');
            $pagination.empty();
            $('.sort-container').show();
            $('.filter-sort-options').removeClass("hide");
            $('.searchDoc').removeClass("hide");
            $('.searchDoc').addClass("show");
            $('.searchDoc').show();

             

            if (data.success && data.results.length > 0) {




                var html = '';


                console.log(data);


                if (data.searchSelection == "fuzzy") {
                    html += '<p>Did you mean <b>' + data.didYouMean + '</b> ?</p>';

                    html += '<p>Found ' + data.totalResults + ' result(s) for <b>"' + data.didYouMean + '"<b></p>';
                }
                else {
                    html += '<p style="margin-top: 10px; margin-left: 22px;">Found ' + data.totalResults + ' result(s) for <b>"' + currentQuery + '"<b></p>';

                    html += '<div class="doc-list-search"><div class="doc-header"><div class="doc-title">Document Name</div><div class="doc-date">Published Date</div></div>';
                }
                $.each(data.results, function (i, item) {
                    console.log(data);
                    console.log(item);

                    var description = item.bodyContent;

                    //if (!description.includes('<mark')) {
                    //    description = "";
                    //}


                   


                    html += '<a href="'+item.url+'" class="doc-item" style="text-decoration:none; cursor:pointer;" data-title="'+item.title+'">';
                    html += ' <div class="doc-title" style="text-decoration:none; color:rgb(51, 51, 51);">' + item.title + '</div><div class="doc-date">' + item.updateDate + '</div></a >';
                });





















                //html += '<div class="result-item"';
                //html += `<p>Page ${data.currentPage} of ${data.totalPages}</p>`;
                html += '</div>';
                results.append(html);



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
        },
        error: function () {
            $('#searchResults').html('<p>Search failed.</p>');
        }
    });






});
});






//document.addEventListener("DOMContentLoaded", function () {

//    $('.recent-searches-box').hide();

//    const input = document.getElementById("searchQuery");
//    const suggestionBox = document.getElementById("autocompleteList");
//    let debounce;
//    let currentId = "";


//    input.addEventListener("keyup", function () {
//        clearTimeout(debounce);
//        const query = this.value.trim();
//        var currentId = "";
//        /*var currentCulture = "";*/

//        var element = document.getElementById("help-id");
//        if (element != null) {
//            currentId = document.getElementById("help-id").textContent;
//        }


//        if (query.length < 2) {
//            $('.recomend-list').empty();
//            $('.recomend-list').hide();
//            /* $('.search-container').removeClass("focus");*/
//            $('.recent-searches-box').show();
//            return;
//        }


//        if (currentId == "") {
//            $.ajax({
//                url: '/umbraco/surface/searchsurface/searchRecommendationHelp',
//                method: 'GET',
//                data: {
//                    query: query,
//                    currentId: currentId
//                },
//                success: function (data) {

//                    if (data.length > 0) {

//                        $('.recomend-list').show();
//                        var searchContainer = $('.search-container');
//                        var displayrecomendation = $('.recomend-list');
//                        displayrecomendation.empty();
//                        var html = '';
//                        /* html += ' <h5>Most Searched</h5><div id="mostSearchedItems">';*/

//                        searchContainer.addClass("focus");

//                        for (let i = 0; i < data.length; i++) {
//                            console.log(data[i].bodyContent);
//                            html += ` <a class="most-searched-item" style="text-decoration:none;" href="#${data[i].title}">${data[i].title}</div><p style="margin-top:0px;margin-bottom:20px; padding-left:8px;">${data[i].bodyContent}</p>`;
//                        }
//                        html += '</div>';

//                        displayrecomendation.html(html);

//                        //console.log('Suggestions:', data);
//                        //Render suggestion list
//                    }
//                },
//                error: function (xhr, status, error) {
//                    console.error('Error:', error);
//                }
//            });
//        }

//});


function getManualfromRecomended(element) {
    // Get the clicked title text (trim to avoid whitespace issues)
    const clickedTitle = element.querySelector('.hidden-manual').textContent;

    /*$('#searchOverlay').hide();*/
    $('.search-container').removeClass('focus');
    $('.most-searched-dropdown').hide();
    $('.ghost-container').hide();


    // Hide all content-section2 blocks
    document.querySelectorAll('.main-content-area2 .content-section2').forEach(section => {
        $('#searchOverlay').hide();


        section.classList.remove('show');
        section.classList.add('hide');
    });

    // Show the content block that matches the clicked title
    const matchedSection = document.getElementById(clickedTitle);
    if (matchedSection) {
        matchedSection.classList.remove('hide');
        matchedSection.classList.add('show');
    }
}
function getManual(element) {
    // Get the clicked title text (trim to avoid whitespace issues)
    const clickedTitle = element.querySelector('.manual-direct').textContent.trim();

    // Hide all content-section2 blocks
    document.querySelectorAll('.main-content-area2 .content-section2').forEach(section => {
        section.classList.remove('show');
        section.classList.add('hide');
    });

    // Show the content block that matches the clicked title
    const matchedSection = document.getElementById(clickedTitle);
    if (matchedSection) {
        matchedSection.classList.remove('hide');
        matchedSection.classList.add('show');
    }
}





$(function () {
     const path = window.location.pathname;
    const fullpath = window.location.href;

    const now = new Date();

    const options = { day: 'numeric', month: 'long', year: 'numeric' };
    const formattedDate = now.toLocaleDateString('en-GB', options);

    var currentCulture = document.getElementById("currentCultureSpan").textContent;

    // Only save if it's a subpage of /manuals for pbb
    if (path.startsWith('/manual/') && path !== '/manuals') {
        const currentManual = {
            url: fullpath,
            title: document.getElementById("manual-title").textContent,
            folder: document.getElementById("manual-folder").textContent,
            date: formattedDate
        };

        if (currentCulture == "pbb") {

            let recent = JSON.parse(localStorage.getItem('recentManuals') || '[]');

            console.log(recent);
            // Remove duplicate
            recent = recent.filter(m => m.title !== currentManual.title);
            // Add new to front
            recent.unshift(currentManual);

            // Limit to 5
            if (recent.length > 5) recent = recent.slice(0, 10);

            localStorage.setItem('recentManuals', JSON.stringify(recent));

        }

    }


    // Only save if it's a subpage of /manuals for pibb
    else if (path.startsWith('/pibb-manual/') && path !== '/pibb-manuals') {
        const currentManual = {
            url: fullpath,
            title: document.getElementById("manual-title").textContent,
            folder: document.getElementById("manual-folder").textContent,
            date: formattedDate
        };





        if (currentCulture == "pibb") {

            let recent = JSON.parse(localStorage.getItem('recentManualsPibb') || '[]');

            console.log(recent);
            // Remove duplicate
            recent = recent.filter(m => m.title !== currentManual.title);
            // Add new to front
            recent.unshift(currentManual);

            // Limit to 5
            if (recent.length > 5) recent = recent.slice(0, 10);

            localStorage.setItem('recentManualsPibb', JSON.stringify(recent));

        }
    }
});






    const starredManuals = [
    {title: "Default Policy", url: "/manuals/default-policy" },
    {title: "Loan Procedures", url: "/manuals/loan-procedures" },
    {title: "IT Guidelines", url: "/manuals/it-guidelines" }
    ];

function populateStarredManuals() {

    currentCulture = document.getElementById("currentCultureSpan").textContent;


    if (currentCulture == "pbb") {

        const starred = JSON.parse(localStorage.getItem('starredManuals') || '[]');

        if (starred.length === 0) {
            /*container.html('<p>No starred documents.</p>');*/
            return;
        }

        const ul = document.querySelector(".starred-manuals-list");
        ul.innerHTML = "";
        starred.forEach(id => {
            $.get(`/umbraco/api/starred/getmanualcontentbyid?id=${id}`, function (res) {
                // Append result to container
                const li = document.createElement("li");
                li.innerHTML = `<a href="${res.url}" style="text-decoration: none; color: #333;">⭐ ${res.name}</a>`;
                ul.appendChild(li);
            });
        });



    }


    else if (currentCulture == "pibb") {

        const starred = JSON.parse(localStorage.getItem('starredManualsPibb') || '[]');

        if (starred.length === 0) {
            /*container.html('<p>No starred documents.</p>');*/
            return;
        }

        const ul = document.querySelector(".starred-manuals-list");
        ul.innerHTML = "";
        starred.forEach(id => {
            $.get(`/umbraco/api/starred/getmanualcontentbyid?id=${id}`, function (res) {
                // Append result to container
                const li = document.createElement("li");
                li.innerHTML = `<a href="${res.url}" style="text-decoration: none; color: #333;">⭐ ${res.name}</a>`;
                ul.appendChild(li);
            });
        });



    }

    }

    // On page load
document.addEventListener("DOMContentLoaded", populateStarredManuals);

document.addEventListener("DOMContentLoaded", function () {
    const sendBtn = document.getElementById("sendBtn");
    const userInput = document.getElementById("userInput");
    const chatBox = document.getElementById("chatBox");

    if (!sendBtn) {
        console.error("sendBtn not found in DOM!");
        return;
    }

    sendBtn.addEventListener("click", async () => {
        let msg = userInput.value.trim();
        if (!msg) return;

        let reply = await sendToAI(msg);

        chatBox.innerHTML += `
            <div><b>You:</b> ${msg}</div>
            <div><b>AI:</b> ${reply}</div>
        `;
    });
});


async function sendToAI(message) {
    let res = await fetch("/umbraco/api/chat/chat", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ query: message })
    });
    let data = await res.json();
    return data.reply;
}





