(function () {


    var app = angular.module('wdf.home', ['ngRoute']);

    app.controller('homeController', ['$scope', function ($scope) {
        document.addEventListener("deviceready", function () {

            //launches login modal
            $scope.getStarted = function () {
                $('#loginModal').modal();
            };
        });
    }]);


})();