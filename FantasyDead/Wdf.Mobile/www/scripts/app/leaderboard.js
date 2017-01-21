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

                    if (response.data.length !== 0) {
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
                    }
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


            //search for a friend
            $scope.searching = false;
            $scope.search = function () {
                var s = $scope.friendSearch;
                if (s === undefined)
                    return;

                $scope.searchResults = [];
                     
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
                $scope.friendSearch = '';
                $scope.searchResults = [];
                $('#friendModal').modal('toggle');
            };

            $scope.friendWork = false;
            //adds a friend
            $scope.addFriend = function (f) {
                $scope.friendWork = true;


                $rootScope.authorize(undefined, true);
                $http.put($rootScope.fdApi + 'api/person/friend/' + f.PersonId, null).then(function (response) {
                    $rootScope.user.Friends.push(f.PersonId);
                    if ($scope.options === null) {
                        $scope.searchModal();
                    } else {
                        $scope.hideOptions();
                    }

                    if ($scope.showing === 'friends') {
                        $scope.leaderboard = [];
                        $scope.leaderboardReady = false;
                        $scope.getLeaderboardPage();
                    }

                    $scope.friendWork = false;
                }).catch(function (error) {
                    $rootScope.handleError(error);
                    $rootScope.showError(error);
                });
            };

            //removes a friend
            $scope.removeFriend = function (id) {
                $scope.friendWork = true;


                $http.delete($rootScope.fdApi + 'api/person/friend/' + id).then(function (response) {
                    if ($scope.showing === 'friends') {
                        $scope.leaderboard = [];
                        $scope.leaderboardReady = false;
                        $scope.getLeaderboardPage();
                    }
                    $scope.hideOptions();

                    $rootScope.authorize(undefined, true);
                    $scope.friendWork = false;
                }).catch(function (error) {
                    $rootScope.handleError(error);
                    $rootScope.showError(error);
                });

            };


            $scope.options = null;
            $scope.showOptions = function (p) {
                $('#optionsModal').modal();
                $scope.options = { person: p, score: $rootScope.generatePointValue(p.TotalScore), isAFriend: $rootScope.isAFriend(p.PersonId) };
            };

            $scope.hideOptions = function () {
                $scope.options = null;
                $('#optionsModal').modal('hide');
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