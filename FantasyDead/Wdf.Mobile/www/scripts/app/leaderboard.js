(function () {
    var app = angular.module('wdf.leaderboard', ['ngRoute']);

    app.controller('leaderboardController', ['$scope', '$rootScope', '$location', '$http', function ($scope, $rootScope, $location, $http) {
        document.addEventListener("deviceready", function () {
            $http.defaults.headers.common.Authorization = 'Bearer ' + $rootScope.user.Token;

            $scope.leaderboard = [];
            $scope.showing = 'all';
            $scope.fetching = false;
            //fetches a new leaderboar page
            $scope.getLeaderboardPage = function (contToken) {
                $scope.fetching = true;
                var path = 'api/statistics/leaderboard/' + $scope.showing;

                var tok = { Token: contToken };
                $http.post($rootScope.fdApi + path, tok).then(function (response) {

                    var rankMod = $scope.leaderboard.length;
                    $.each(response.data.Items, function (ix, i) {

                        i.CurrentRank += rankMod;
                        i.PreviousRank += rankMod;

                        i.deltaIcon = '';
                        if (i.CurrentRank > i.PreviousRank)
                            i.deltaIcon = 'fa-caret-down';
                        if (i.CurrentRank < i.PreviousRank)
                            i.deltaIcon = 'fa-caret-up';

                        $scope.lastWeekName = i.EpisodeScores[0].EpisodeName;

                        if (i.EpisodeScores.length > 1) {
                            $scope.twoWeeksAgoName = i.EpisodeScores[1].EpisodeName;
                        }

                        $scope.leaderboard.push(i);
                    });

                    $scope.lbContToken = response.data.ContinuationToken;
                    $scope.fetching = false;
                    $scope.leaderboardReady = true;
                }).catch(function (error) {
                    $rootScope.handleError(error);
                    $rootScope.showError('There was an issue loading the leaderboard.');
                });

            };


            $scope.toggleLb = function (mode) {
                $scope.leaderboard = [];
                $scope.lbContToken = null;
                $scope.leaderboardReady = false;
                $scope.showing = mode;
                $scope.getLeaderboardPage();
            }

            //init page
            $scope.leaderboardReady = false;
            $scope.init = function () {
                //fetch leaderboard
                $scope.getLeaderboardPage();
            };


            $scope.init();

        });
    }]);


})();