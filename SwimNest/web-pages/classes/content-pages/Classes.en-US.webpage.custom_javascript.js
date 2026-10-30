// This function will display a Hello World message when the page is fully loaded
// Uses jQuery 3.6.2 as provided by Power Pages
$(document).ready(function () {
    // Use alert for demonstration; consider accessible alternatives for production
    alert('Hello World');
    var randomNumber = generateRandomNumber();
      alert('Random Number: ' + randomNumber);


      function generateRandomNumber() {
    return Math.floor(Math.random() * 100) + 1;
}
});





