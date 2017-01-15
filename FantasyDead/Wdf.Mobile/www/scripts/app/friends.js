(function () {
    var app = angular.module('wdf.friends', ['ngRoute']);

    app.controller('friendsController', ['$scope', '$rootScope', '$location', '$http', function ($scope, $rootScope, $location, $http) {
        document.addEventListener("deviceready", function () {
            $http.defaults.headers.common.Authorization = 'Bearer ' + $rootScope.user.Token;

            $scope.currentlyFlipped = null;
            $scope.flip = function (id) {
                $scope.currentlyFlipped = id;
            };

            //search for a friend
            $scope.searching = false;
            $scope.search = function () {

                var s = $scope.friendSearch;
                $scope.searching = true;
                $http.get($rootScope.fdApi + 'api/person/friend/search/' + s).then(function (response) {
                    $scope.searchResults = response.data;
                    $scope.searching = false;
                }).catch(function (error) {
                    $scope.searching = false;
                    $rootScope.handleError(error);
                    $rootScope.showError(error);
                });
            };

            //toggle search modal
            $scope.searchModal = function () {
                $('#friendModal').modal('toggle');
            };

            //adds a friend
            $scope.addFriend = function (f) {

                $scope.friends.push(f);
                $scope.searchModal();
                $http.put($rootScope.fdApi + 'api/person/friend/' + f.PersonId, null).then(function (response) {

                }).catch(function (error) {
                    $rootScope.handleError(error);
                    $rootScope.showError(error);
                });
            };

            //removes a friend
            $scope.removeFriend = function (id) {

                $http.delete($rootScope.fdApi + 'api/person/friend/' + id).then(function (response) {
                    $scope.init();
                }).catch(function (error) {
                    $rootScope.handleError(error);
                    $rootScope.showError(error);
                });

            };



            $scope.ready = false;
            //init page   
            $scope.init = function () {
                $scope.ready = false;
                $http.post($rootScope.fdApi + 'api/statistics/leaderboard/friends').then(function (response) {
                    $scope.friends = response.data.Items;
                    $scope.ready = true;
                }).catch(function (error) {
                    $rootScope.handleError(error);
                    $rootScope.showError(error);
                });

            };


            $scope.init();

        });
    }]);


})();