function toggleDropdown() {
    var dropdown = document.getElementById("myDropdown");
    dropdown.style.display = dropdown.style.display === "block" ? "none" : "block";
}

document.addEventListener("DOMContentLoaded", function () {
    var nestedDropdown = document.querySelector(".nested-dropdown");
    var nestedDropdownContent = document.querySelector(".nested-dropdown-content");

    nestedDropdown.addEventListener("mouseenter", function () {
        nestedDropdownContent.style.display = "block";
    });

    nestedDropdown.addEventListener("mouseleave", function () {
        nestedDropdownContent.style.display = "none";
    });
});
// Close the dropdown if the user clicks outside of it
document.addEventListener("click", function (event) {
    var dropdown = document.getElementById("myDropdown");

    // Check if the clicked element is not a part of the dropdown
    if (!event.target.closest(".dropdown-btn")) {
        dropdown.style.display = "none";
    }
});