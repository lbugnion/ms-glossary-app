function checkSubtopic(subtopic) {

    if (subtopic.length > 0) {

        var h1 = document.getElementsByTagName("h1")[0];

        var div = document.createElement("div");
        div.className = "redirected";
        div.innerText = "(redirected from " + subtopic + ")";

        h1.parentNode.insertBefore(div, h1.nextElementSibling);
    }
}