(function () {


    var app = angular.module('wdf.home', ['ngRoute']);

    app.controller('homeController', ['$scope', '$rootScope', '$location', function ($scope, $rootScope, $location) {
        document.addEventListener("deviceready", function () {


            if ($rootScope.user.loggedIn)
                $location.path('/roster');

            //launches login modal
            $scope.getStarted = function () {
                $('#loginModal').modal();
            };
        });
    }]);


})();