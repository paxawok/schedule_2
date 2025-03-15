document.addEventListener("DOMContentLoaded", function () {
    document.querySelectorAll(".calendar-cell").forEach(cell => {
        cell.addEventListener("click", function () {
            let title = prompt("Enter event name:");
            if (title) {
                let eventDiv = document.createElement("div");
                eventDiv.classList.add("event");
                eventDiv.innerText = title;
                cell.appendChild(eventDiv);
            }
        });
    });
});
